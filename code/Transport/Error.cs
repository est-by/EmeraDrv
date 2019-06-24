using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sys.Services.Drv.Emera
{
  class Error
  {
    public readonly string Operand;
    public readonly string Description;
    public readonly string Info;
    public Error(string op, string desc, string info)
    {
      this.Operand = op;
      this.Description = desc;
      this.Info = info;
    }

    static List<Error> errors = new List<Error>();
    public static bool IsError(byte[] buff, out Error err)
    {
      if (errors.Count == 0)
      {
        errors.Add(new Error("ERROR01", "Операция не поддерживается", "  Операция отсутствует в списке операций счетчика"));
        errors.Add(new Error("ERROR02", "Ошибка контрольной суммы ", " При обмене через порт связи произошла ошибка контрольной суммы"));
        errors.Add(new Error("ERROR03", "Неверный пароль ", "При программировании введен пароль, не совпадающий с внутренним паролем счетчика"));
        errors.Add(new Error("ERROR04", "Неправильный формат данных ", "Сообщение, полученное счетчиком через порт связи, синтаксически неправильно"));
        errors.Add(new Error("ERROR05", "Доступ запрещен ", "Запись параметров счетчика в данном режиме запрещена"));
        errors.Add(new Error("ERROR06", "Неизвестная команда ", "Команда отсутствует в списке команд счетчика"));
        errors.Add(new Error("ERROR07", "Превышен архивный индекс  ", "Превышен диапазон допустимых архивных индексов для счетчика"));
        errors.Add(new Error("ERROR08", "Нет архивных данных ", "По заданному индексу нет данных"));
        errors.Add(new Error("ERROR09", "Запись запрещена ", "Данная команда не предназначена для записи параметров"));
        errors.Add(new Error("ERROR10", "Чтение запрещено ", "Данная команда не предназначена для чтения параметров"));
        errors.Add(new Error("ERROR11", "Неправильный формат даты(времени) ", "На запись в счетчик была послана команда с неправильным форматом даты(времени)"));
        errors.Add(new Error("ERROR12", "Ошибка часов счетчика ", "Ошибка при записи(корректировке) времени часов"));
        errors.Add(new Error("ERROR13", "Ошибка EEPROM ", "Ошибка при записи параметров во внутреннюю память счетчика"));
        errors.Add(new Error("ERROR14", "Неправильные параметры  ", "Переданы недопустимые значения для записи параметра"));
        errors.Add(new Error("ERROR15", "Превышен лимит коррекции времени ", "Превышен допустимый лимит коррекции времени для данного временного интервала"));
        errors.Add(new Error("ERROR16", "Превышен лимит ввода неправильного пароля ", "Счетчик заблокирован до окончания суток из-за трех попыток неправильного ввода пароля"));
        errors.Add(new Error("ERROR17", "Запрошенный интервал отсутствует ", "Попытка чтения 3(30) - минутной мощность за предыдущий интервал до появления его в счетчике после включения"));
        errors.Add(new Error("ERROR18", "Ошибка при расчете значений ", "Ошибка при вычислении коэффициента мощности"));
        errors.Add(new Error("ERROR19", "Операция запрещена", "Выполнение операции запрещено конфигурацией счетчика"));
      }
      err = null;
      if (new ASCIIChar(buff[1]) == ASCIIChars.E && new ASCIIChar(buff[2]) == ASCIIChars.R && new ASCIIChar(buff[3]) == ASCIIChars.R && new ASCIIChar(buff[4]) == ASCIIChars.O && new ASCIIChar(buff[5]) == ASCIIChars.R)
      {
        string s = new ASCIIString(buff, 1, buff.Length - 1).ToString();
        int idx = s.IndexOf("ERROR");
        s = s.Substring(idx,5 + 2);
        err = errors.Find(x => x.Operand == s);
        return true;
      }
      return false;
    }

  }
}
