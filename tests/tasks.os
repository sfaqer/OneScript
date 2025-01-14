Перем юТест;

Перем глБлокировка;
Перем глСумма;

////////////////////////////////////////////////////////////////////
// Программный интерфейс

Функция ПолучитьСписокТестов(ЮнитТестирование) Экспорт
	
	юТест = ЮнитТестирование;
	
	ВсеТесты = Новый Массив;
	
	ВсеТесты.Добавить("ТестДолжен_ПроверитьСозданиеЗаданий");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьПоискЗаданий");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьОжиданиеВсехЗаданий");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьОжиданиеЛюбогоИзЗаданий");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьОжиданиеКонкретногоЗадания");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьЧтоВЗаданииПроставленаИнформацияОбОшибке");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьЧтоВозвращаетсяРезультат");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьЧтоВозвращаетсяРезультатДелегата");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьЧтоРаботаетБлокировка");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьЧтоКодМожетОпределитьИДЗадания");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьЧтоВИнформацииОбОшибкеЕстьСтекВызовов");
	
	Возврат ВсеТесты;
	
КонецФункции

Процедура ПередЗапускомТеста() Экспорт
	ФоновыеЗадания.Очистить();
КонецПроцедуры

Процедура ПроцедураБезПараметров() Экспорт
	Приостановить(500);
КонецПроцедуры

Процедура ПроцедураСПараметрами(Интервал) Экспорт
	Приостановить(Интервал);
КонецПроцедуры

Процедура ПроцедураСИсключением() Экспорт
	Приостановить(500);
	ВызватьИсключение "Я ошибка в коде";
КонецПроцедуры

Функция ПодсчетСуммы(Знач Начало, Знач Конец) Экспорт
	
	//дадим всем заданиям шанс начаться
	Приостановить(500);

	СуммаНаОтрезке = 0;
	Для Сч = Начало По Конец Цикл
		СуммаНаОтрезке = СуммаНаОтрезке + Сч;
	КонецЦикла;

	Возврат СуммаНаОтрезке;

КонецФункции

Процедура ПодсчетГлобальнойСуммы(Знач Начало, Знач Конец) Экспорт
	
	СуммаНаОтрезке = ПодсчетСуммы(Начало, Конец);

	глБлокировка.Заблокировать();
	глСумма = глСумма + СуммаНаОтрезке;
	глБлокировка.Разблокировать();
	
КонецПроцедуры

Процедура ТестДолжен_ПроверитьСозданиеЗаданий() Экспорт
	
	Задание = ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроцедураБезПараметров");
	юТест.ПроверитьИстину(ЗначениеЗаполнено(Задание.УникальныйИдентификатор));
	юТест.ПроверитьРавенство("ПроцедураБезПараметров", Задание.ИмяМетода);
	юТест.ПроверитьРавенство(ЭтотОбъект, Задание.Объект);
	юТест.ПроверитьРавенство(Неопределено, Задание.Параметры);
	
	Задание.ОжидатьЗавершения();

	юТест.ПроверитьРавенство(СостояниеФоновогоЗадания.Завершено, Задание.Состояние);

КонецПроцедуры

Процедура ТестДолжен_ПроверитьПоискЗаданий() Экспорт
	
	З = ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроцедураБезПараметров");
	ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроцедураБезПараметров");
	ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроцедураБезПараметров");
	ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроцедураБезПараметров");

	Все = ФоновыеЗадания.ПолучитьФоновыеЗадания();
	юТест.ПроверитьРавенство(4, Все.Количество());

	ПоИд = ФоновыеЗадания.ПолучитьФоновыеЗадания(Новый Структура("UUID", З.УникальныйИдентификатор));
	юТест.ПроверитьРавенство(1, ПоИд.Количество());

КонецПроцедуры

Процедура ТестДолжен_ПроверитьОжиданиеВсехЗаданий() Экспорт
	
	МассивПараметров = Новый Массив;
	МассивПараметров.Добавить(3000);
	
	МассивЗаданий = Новый Массив;
	
	МассивЗаданий.Добавить(ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроцедураСПараметрами", МассивПараметров));
	МассивЗаданий.Добавить(ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроцедураБезПараметров"));
	
	ФоновыеЗадания.ОжидатьВсе(МассивЗаданий);

	Завершенные = ФоновыеЗадания.ПолучитьФоновыеЗадания(Новый Структура("Состояние", СостояниеФоновогоЗадания.Завершено));
	юТест.ПроверитьРавенство(2, Завершенные.Количество());

КонецПроцедуры

Процедура ТестДолжен_ПроверитьОжиданиеЛюбогоИзЗаданий() Экспорт
	
	МассивПараметров = Новый Массив;
	МассивПараметров.Добавить(3000);
	
	МассивЗаданий = Новый Массив;
	
	МассивЗаданий.Добавить(ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроцедураСПараметрами", МассивПараметров));
	МассивЗаданий.Добавить(ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроцедураБезПараметров"));
	
	ФоновыеЗадания.ОжидатьЛюбое(МассивЗаданий);

	Завершенные = ФоновыеЗадания.ПолучитьФоновыеЗадания(Новый Структура("Состояние", СостояниеФоновогоЗадания.Завершено));
	юТест.ПроверитьМеньшеИлиРавно(Завершенные.Количество(), 2);

КонецПроцедуры

Процедура ТестДолжен_ПроверитьОжиданиеКонкретногоЗадания() Экспорт
	
	Задание = ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроцедураБезПараметров");
	Задание.ОжидатьЗавершения();

	юТест.ПроверитьРавенство(СостояниеФоновогоЗадания.Завершено, Задание.Состояние);

КонецПроцедуры

Процедура ТестДолжен_ПроверитьЧтоВЗаданииПроставленаИнформацияОбОшибке() Экспорт
	
	Задание = ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроцедураСИсключением");
	Задание.ОжидатьЗавершения();

	юТест.ПроверитьРавенство(СостояниеФоновогоЗадания.ЗавершеноАварийно, Задание.Состояние);
	юТест.ПроверитьРавенство("Я ошибка в коде", Задание.ИнформацияОбОшибке.Описание);

КонецПроцедуры

Процедура ТестДолжен_ПроверитьЧтоВозвращаетсяРезультат() Экспорт
	
	МассивПараметров = Новый Массив(2);
	МассивПараметров[0] = 1;
	МассивПараметров[1] = 100;

	ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПодсчетСуммы", МассивПараметров);

	МассивПараметров[0] = 101;
	МассивПараметров[1] = 200;

	ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПодсчетСуммы", МассивПараметров);

	МассивПараметров[0] = 201;
	МассивПараметров[1] = 300;

	ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПодсчетСуммы", МассивПараметров);

	ВсеЗадания = ФоновыеЗадания.ПолучитьФоновыеЗадания();
	ФоновыеЗадания.ОжидатьВсе(ВсеЗадания);

	Сумма = 0;
	Для Каждого Задание Из ВсеЗадания Цикл
		Сумма = Сумма + Задание.Результат;
	КонецЦикла;

	юТест.ПроверитьРавенство(45150, Сумма);

КонецПроцедуры

Процедура ТестДолжен_ПроверитьЧтоВозвращаетсяРезультатДелегата() Экспорт
	
    Делегат = Новый Действие(ЭтотОбъект, "ПодсчетСуммы");

	МассивПараметров = Новый Массив(2);
	МассивПараметров[0] = 1;
	МассивПараметров[1] = 5;

	ФЗ = ФоновыеЗадания.Выполнить(Делегат, "Выполнить", МассивПараметров);

	ФЗ.ОжидатьЗавершения();

	юТест.ПроверитьРавенство(15, ФЗ.Результат);

КонецПроцедуры

Процедура ТестДолжен_ПроверитьЧтоРаботаетБлокировка() Экспорт
	
	глБлокировка = Новый БлокировкаРесурса;
	глСумма = 0;
	
	МассивПараметров = Новый Массив(2);
	МассивПараметров[0] = 1;
	МассивПараметров[1] = 1000;

	ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПодсчетГлобальнойСуммы", МассивПараметров);

	МассивПараметров[0] = 1001;
	МассивПараметров[1] = 2000;

	ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПодсчетГлобальнойСуммы", МассивПараметров);

	МассивПараметров[0] = 2001;
	МассивПараметров[1] = 3000;

	ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПодсчетГлобальнойСуммы", МассивПараметров);

	Попытка
		ФоновыеЗадания.ОжидатьЗавершенияЗадач();
	Исключение
		МассивОшибок = ИнформацияОбОшибке().Параметры;
		Если МассивОшибок <> Неопределено Тогда
			Для Каждого Задание Из МассивОшибок Цикл
				Сообщить(Задание.ИнформацияОбОшибке.ПодробноеОписаниеОшибки());
			КонецЦикла;
		КонецЕсли;
	КонецПопытки;

	юТест.ПроверитьРавенство(4501500, глСумма);

КонецПроцедуры

Функция ПроверитьИдентификаторЗадания() Экспорт
	
	Приостановить(500);
	Задание = ФоновыеЗадания.ПолучитьТекущее();
	Если Задание <> Неопределено Тогда
		Возврат Задание.УникальныйИдентификатор;
	КонецЕсли;
	
	Возврат Неопределено;
	
КонецФункции

Процедура ТестДолжен_ПроверитьЧтоКодМожетОпределитьИДЗадания() Экспорт
	
	МассивЗаданий = Новый Массив;
	МассивЗаданий.Добавить(ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроверитьИдентификаторЗадания"));
	МассивЗаданий.Добавить(ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроверитьИдентификаторЗадания"));
	МассивЗаданий.Добавить(ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроверитьИдентификаторЗадания"));
	МассивЗаданий.Добавить(ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроверитьИдентификаторЗадания"));
	
	ФоновыеЗадания.ОжидатьВсе(МассивЗаданий);
	
	Для Каждого Задание Из МассивЗаданий Цикл
		юТест.ПроверитьРавенство(Задание.УникальныйИдентификатор, Задание.Результат);
		юТест.ПроверитьНеРавенство(Неопределено, Задание.Результат);
	КонецЦикла;
	
	ЗаданиеГлавногоПотока = ФоновыеЗадания.ПолучитьТекущее();
	юТест.ПроверитьРавенство(Неопределено, ЗаданиеГлавногоПотока);

КонецПроцедуры

Процедура ТестДолжен_ПроверитьЧтоВИнформацииОбОшибкеЕстьСтекВызовов() Экспорт

	Задание = ФоновыеЗадания.Выполнить(ЭтотОбъект, "ПроцедураСИсключением");
	Задание.ОжидатьЗавершения();

	СтекВызовов = Задание.ИнформацияОбОшибке.ПолучитьСтекВызовов();

	юТест.ПроверитьТип(СтекВызовов, "КоллекцияКадровСтекаВызовов");

	юТест.ПроверитьРавенство(СтекВызовов.Количество(), 1);

	юТест.ПроверитьРавенство(СтекВызовов[0].Метод, "ПроцедураСИсключением");
	юТест.ПроверитьРавенство(СтекВызовов[0].НомерСтроки, 45);
	юТест.ПроверитьРавенство(СтекВызовов[0].ИмяМодуля, ТекущийСценарий().Источник);

КонецПроцедуры
