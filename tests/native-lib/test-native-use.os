#native
#Использовать "."

Перем юТест;

Функция ПолучитьСписокТестов(Тестирование) Экспорт
    
    юТест = Тестирование;
    
    Тесты = Новый Массив;
    Тесты.Добавить("ТестДолжен_ПроверитьСозданиеОбъекта_ИзНативногоКласса");
    Тесты.Добавить("ТестДолжен_ПроверитьПодключениеСценария");
    
    Возврат Тесты;
    
КонецФункции

Процедура ТестДолжен_ПроверитьСозданиеОбъекта_ИзНативногоКласса() Экспорт
    ТестовыйСценарий = Новый ТестовыйСценарий;
КонецПроцедуры

Процедура ТестДолжен_ПроверитьПодключениеСценария() Экспорт
    ПодключитьСценарий(ОбъединитьПути(ТекущийСценарий().Каталог, "Классы", "ТестовыйСценарий.os"), "ПодключенныйСценарий");
    
    ТестовыйСценарий = Новый ПодключенныйСценарий;
КонецПроцедуры