/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using System.Collections;
using System.Collections.Generic;
using OneScript.Commons;
using OneScript.Compilation;
using OneScript.Compilation.Binding;
using OneScript.Contexts;
using OneScript.Execution;
using ScriptEngine.Libraries;
using ScriptEngine.Machine;
using SymbolScope = OneScript.Compilation.Binding.SymbolScope;

namespace ScriptEngine
{
    [Obsolete("Use interface IRuntimeEnvironment")]
    public class RuntimeEnvironment : IRuntimeEnvironment, ILibraryManager
    {
        private readonly SymbolTable _symbols = new SymbolTable();
        private SymbolScope _scopeOfGlobalProperties;
        
        private readonly PropertyBag _injectedProperties;

        // Глобальные области читают компиляторы и машины других потоков, пока подключается
        // компонента или загружается библиотека. Поэтому им отдаются копии, которые заменяются целиком
        private volatile SymbolTable _publishedSymbols = new SymbolTable();
        private volatile IAttachableContext[] _contexts = Array.Empty<IAttachableContext>();

        private readonly ILibraryManager _libraryManager;

        public RuntimeEnvironment()
        {
            _injectedProperties = new PropertyBag();
            _libraryManager = new LibraryManager(_injectedProperties);
            AttachedContexts = new AttachedContextsView(this);
        }

        private void CreateGlobalScopeIfNeeded()
        {
            if (_scopeOfGlobalProperties != null) 
                return;
            
            lock (_injectedProperties)
            {
                if (_scopeOfGlobalProperties != null)
                    return;

                // Сюда добавляются модули библиотек, пока другие потоки компилируют сценарии
                var scope = new SymbolScope(concurrentReads: true);
                _symbols.PushScope(scope, ScopeBindingDescriptor.Static(_injectedProperties));
                PublishScopes(_injectedProperties);
                _scopeOfGlobalProperties = scope;
            }
        }

        // Вызывается под блокировкой, после того как область добавлена в _symbols
        private void PublishScopes(IAttachableContext addedContext)
        {
            var contexts = new IAttachableContext[_contexts.Length + 1];
            _contexts.CopyTo(contexts, 0);
            contexts[^1] = addedContext;
            _contexts = contexts;

            var table = new SymbolTable();
            for (int i = 0; i < _symbols.ScopeCount; i++)
            {
                table.PushScope(_symbols.GetScope(i), _symbols.GetBinding(i));
            }
            _publishedSymbols = table;
        }

        public void InjectObject(IAttachableContext context)
        {
            RegisterObject(context);
        }

        public void InjectGlobalProperty(IValue value, string identifier, string alias, bool readOnly)
        {
            InjectPropertyInternal(value, identifier, alias, readOnly, null);
        }
        
        public void InjectGlobalProperty(IValue value, string identifier, bool readOnly)
        {
            InjectGlobalProperty(value, identifier, default, readOnly);
        }

        public void InjectGlobalProperty(IValue value, string identifier, PackageInfo ownerPackage)
        {
            InjectPropertyInternal(value, identifier, default, true, ownerPackage);
        }

        private void InjectPropertyInternal(
            IValue value,
            string identifier,
            string alias,
            bool readOnly,
            PackageInfo ownerPackage)
        {
            ArgumentNullException.ThrowIfNull(value);
            if(!Utils.IsValidIdentifier(identifier))
            {
                throw new ArgumentException("Invalid identifier", nameof(identifier));
            }

            if (alias != default && !Utils.IsValidIdentifier(alias))
            {
                throw new ArgumentException("Invalid identifier", nameof(alias));
            }
            CreateGlobalScopeIfNeeded();
            var num = _injectedProperties.Insert(value, identifier, true, !readOnly);

            var bslPropertyInfo = _injectedProperties.GetPropertyInfo(num);
            IVariableSymbol registeredSymbol;
            if (ownerPackage == null)
            {
                registeredSymbol = new WrappedPropertySymbol(bslPropertyInfo)
                {
                    Name = identifier,
                    Alias = alias
                };
            }
            else
            {
                registeredSymbol = new WrappedLibraryPropertySymbol(bslPropertyInfo, ownerPackage)
                {
                    Name = identifier,
                    Alias = alias
                };
            }

            _scopeOfGlobalProperties.DefineVariable(registeredSymbol);
        }

        public void InjectGlobalProperty(IValue value, BslPropertyInfo definition)
        {
            CreateGlobalScopeIfNeeded();
            _injectedProperties.Insert(value, definition);

            var symbol = new WrappedPropertySymbol(definition)
            {
                Name = definition.Name,
                Alias = definition.Alias
            };

            _scopeOfGlobalProperties.DefineVariable(symbol);
        }

        private void RegisterObject(IAttachableContext context)
        {
            lock (_injectedProperties)
            {
                _symbols.PushContext(context);
                PublishScopes(context);
            }
        }
        
        public void SetGlobalProperty(string propertyName, IValue value)
        {
            _publishedSymbols.FindVariable(propertyName, out var binding);

            var context = _contexts[binding.ScopeNumber];
            context.SetPropValue(binding.MemberNumber, value);
        }

        public IValue GetGlobalProperty(string propertyName)
        {
            _publishedSymbols.FindVariable(propertyName, out var binding);

            var context = _contexts[binding.ScopeNumber];
            return context.GetPropValue(binding.MemberNumber);
        }

        public SymbolTable GetSymbolTable() => _publishedSymbols;

        public IReadOnlyList<IAttachableContext> AttachedContexts { get; }

        public void InitExternalLibrary(ScriptingEngine runtime, ExternalLibraryInfo library, IBslProcess process)
        {
            _libraryManager.InitExternalLibrary(runtime, library, process);
        }

        /// <summary>
        /// Текущий список глобальных контекстов: кадры выполнения держат его и видят контексты,
        /// подключенные после их создания
        /// </summary>
        private sealed class AttachedContextsView : IReadOnlyList<IAttachableContext>
        {
            private readonly RuntimeEnvironment _owner;

            public AttachedContextsView(RuntimeEnvironment owner)
            {
                _owner = owner;
            }

            public int Count => _owner._contexts.Length;

            // Массив только растет, поэтому номер, полученный по прежнему Count, остается верным
            public IAttachableContext this[int index] => _owner._contexts[index];

            public IEnumerator<IAttachableContext> GetEnumerator() => ((IEnumerable<IAttachableContext>)_owner._contexts).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class WrappedPropertySymbol : IPropertySymbol
        {
            public WrappedPropertySymbol(BslPropertyInfo propInfo)
            {
                Property = propInfo;
            }

            public string Name { get; set; }
            public string Alias { get; set; }
            public Type Type => Property.PropertyType;
            public BslPropertyInfo Property { get; }
        }
        
        private class WrappedLibraryPropertySymbol : IPropertySymbol, IPackageSymbol
        {
            public WrappedLibraryPropertySymbol(BslPropertyInfo propInfo, PackageInfo ownerPackage)
            {
                Property = propInfo;
                Package = ownerPackage;
            }

            public string Name { get; set; }
            public string Alias { get; set; }
            public Type Type => Property.PropertyType;
            public BslPropertyInfo Property { get; }
            private PackageInfo Package { get; }

            public PackageInfo GetPackageInfo() => Package;
        }
    }
}
