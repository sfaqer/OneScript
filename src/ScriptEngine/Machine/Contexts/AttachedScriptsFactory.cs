/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using OneScript.Sources;
using System.Security.Cryptography;
using OneScript.Commons;
using OneScript.Compilation;
using OneScript.Compilation.Binding;
using OneScript.Contexts;
using OneScript.Exceptions;
using OneScript.Execution;
using OneScript.Types;
using ScriptEngine.Machine.Interfaces;

namespace ScriptEngine.Machine.Contexts
{
    public class AttachedScriptsFactory
    {
        // Сценарии подключаются и из фоновых заданий: регистрация под блокировкой,
        // модули читаются без нее при каждом создании объекта
        private readonly ConcurrentDictionary<string, IExecutableModule> _loadedModules;
        private readonly ConcurrentDictionary<string, string> _fileHashes;
        private readonly object _registrationLock = new object();
        
        private readonly ScriptingEngine _engine;

        internal AttachedScriptsFactory(ScriptingEngine engine)
        {
            _loadedModules = new ConcurrentDictionary<string, IExecutableModule>(StringComparer.InvariantCultureIgnoreCase);
            _fileHashes = new ConcurrentDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            _engine = engine;
        }

        private ITypeManager TypeManager => _engine.TypeManager;
        
        // Хеш нужен только чтобы узнать тот же текст модуля при повторном подключении
        private static string GetSourceHash(string code)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
        }

        public void AttachByPath(ICompilerFrontend compiler, string path, string typeName, IBslProcess process)
        {
            if (!Utils.IsValidIdentifier(typeName))
                throw RuntimeException.InvalidArgumentValue();

            var code = _engine.Loader.FromFile(path);
            
            ThrowIfTypeExist(typeName, code);

            CompileAndRegister(typeof(AttachedScriptsFactory), compiler, typeName, code, process);

        }

        public void AttachFromString(ICompilerFrontend compiler, string text, string typeName, IBslProcess process)
        {
            var code = _engine.Loader.FromString(text);
            ThrowIfTypeExist(typeName, code);
            
            CompileAndRegister(typeof(AttachedScriptsFactory), compiler, typeName, code, process);
        }

        public UserScriptContextInstance LoadFromPath(ICompilerFrontend compiler, string path, IBslProcess process)
        {
            return LoadFromPath(compiler, path, null, process);
        }

        public UserScriptContextInstance LoadFromPath(ICompilerFrontend compiler, string path,
            ExternalContextData externalContext, IBslProcess process)
        {
            var code = _engine.Loader.FromFile(path);
            return LoadAndCreate(compiler, code, externalContext, process);
        }

        public UserScriptContextInstance LoadFromString(ICompilerFrontend compiler, string text, IBslProcess process,
            ExternalContextData externalContext = null)
        {
            var code = _engine.Loader.FromString(text);
            return LoadAndCreate(compiler, code, externalContext, process);
        }


        private void ThrowIfTypeExist(string typeName, SourceCode code)
        {
            if (TypeManager.IsKnownType(typeName) && _loadedModules.ContainsKey(typeName))
            {
                string hash = GetSourceHash(code.GetSourceCode());

                // Хеша нет у классов библиотек и у модуля, регистрацию которого откатили
                StringComparer comparer = StringComparer.OrdinalIgnoreCase;
                if(!_fileHashes.TryGetValue(typeName, out var storedHash) || comparer.Compare(hash, storedHash) != 0)
                    throw new RuntimeException("Type «" + typeName + "» already registered");

            }

        }

        private void CompileAndRegister(Type type, ICompilerFrontend compiler, string typeName, SourceCode code, IBslProcess process)
        {
            string hash = GetSourceHash(code.GetSourceCode());

            // Под блокировкой: регистрация из другого потока могла еще не закончиться или откатиться,
            // а после ThrowIfTypeExist тип могли подключить с другим текстом
            lock (_registrationLock)
            {
                if (IsRegisteredWithSameSource(typeName, hash))
                    return;
            }

            var module = CompileModuleFromSource(compiler, code, null, process);

            lock (_registrationLock)
            {
                // Пока модуль компилировался, этот же тип могли подключить из другого потока
                if (IsRegisteredWithSameSource(typeName, hash))
                    return;

                // Хеш раньше модуля: ThrowIfTypeExist читает его, когда модуль уже виден
                _fileHashes[typeName] = hash;
                _loadedModules[typeName] = module;
                try
                {
                    TypeManager.RegisterType(typeName, default, type);
                }
                catch
                {
                    // Имя занято другим типом: без отката повторное подключение молча «удалось» бы
                    _loadedModules.TryRemove(typeName, out _);
                    _fileHashes.TryRemove(typeName, out _);
                    throw;
                }
            }
        }

        // true - тип уже подключен с этим же текстом, другой текст - исключение
        private bool IsRegisteredWithSameSource(string typeName, string hash)
        {
            if (!_loadedModules.ContainsKey(typeName))
                return false;

            if (!_fileHashes.TryGetValue(typeName, out var storedHash)
                || !StringComparer.OrdinalIgnoreCase.Equals(hash, storedHash))
                throw new RuntimeException("Type «" + typeName + "» already registered");

            return true;
        }

        public void RegisterTypeModule(string typeName, IExecutableModule module)
        {
            lock (_registrationLock)
            {
                if (_loadedModules.TryGetValue(typeName, out var loadedModule))
                {
                    var alreadyLoadedSrc = loadedModule.Source.Location;
                    var currentSrc = module.Source.Location;

                    if(alreadyLoadedSrc != currentSrc)
                        throw new RuntimeException("Type «" + typeName + "» already registered");

                    return;
                }

                _loadedModules[typeName] = module;
                try
                {
                    _engine.TypeManager.RegisterType(typeName, default, typeof(AttachedScriptsFactory));
                }
                catch
                {
                    _loadedModules.TryRemove(typeName, out _);
                    throw;
                }
            }
        }
        
        private UserScriptContextInstance LoadAndCreate(ICompilerFrontend compiler, SourceCode code,
            ExternalContextData externalContext, IBslProcess process)
        {
            var module = CompileModuleFromSource(compiler, code, externalContext, process);
            return _engine.NewObject(module, process, externalContext);
        }

        public IExecutableModule CompileModuleFromSource(ICompilerFrontend compiler, SourceCode code, ExternalContextData externalContext, IBslProcess process)
        {
            var scope = compiler.FillSymbols(typeof(UserScriptContextInstance));
            if (externalContext != null)
            {
                foreach (var item in externalContext)
                {
                    scope.Variables.Add(new LocalVariableSymbol(item.Key, item.Value.GetType()));
                }
            }

            return compiler.Compile(code, process);
        }
        
        private static AttachedScriptsFactory _instance;

        static AttachedScriptsFactory()
        {
        }

        internal static void SetInstance(AttachedScriptsFactory factory)
        {
            _instance = factory;
        }

        public static IExecutableModule GetModuleOfType(string typeName)
        {
            return _instance._loadedModules[typeName];
        }

        [ScriptConstructor]
        public static UserScriptContextInstance ScriptFactory(TypeActivationContext context, IValue[] arguments)
        {
            var module = _instance._loadedModules[context.TypeName];

            var type = context.TypeManager.GetTypeByName(context.TypeName);
            UserScriptContextInstance newObj;
            if (module.GetInterface<IterableBslInterface>() != null)
            {
                newObj = new UserIterableContextInstance(module, type, arguments);
            }
            else
            {
                newObj = new UserScriptContextInstance(module, type, arguments);
            }

            newObj.InitOwnData();
            newObj.Initialize(context.CurrentProcess);

            return newObj;
        }

    }
}
