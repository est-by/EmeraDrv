using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Sys.IO;

namespace Sys.Services.Drv.Emera
{
  internal class TL
  {
    class Record
    {
      public string format = "";
      public object[] args = null;
      public DateTime dateTime = DateTime.MaxValue;
    }

    static Queue<Record> msg = new Queue<Record>();

    static StreamWriter writer = new StreamWriter(Folders.GetFullPath("Component.Common.Test.txt"), false);

    static TL()
    {
      writer.AutoFlush = true;
    }

    static bool useTimer = false;
    static System.Threading.Timer t = new System.Threading.Timer((s) =>
    {
      Record[] record;

      lock (msg)
      {
        if (useTimer) return;
        useTimer = true;
        record = msg.ToArray();
        msg.Clear();
      }

      for (int i = 0; i < record.Length; i++)
      {
        string n = record[i].dateTime.ToString("HH:mm:ss.fff");
        Debug.WriteLine("{0}: {1}", n, String.Format(record[i].format, record[i].args));
        writer.WriteLine("{0}: {1}", n, String.Format(record[i].format, record[i].args));
      }
      useTimer = false;
    }, null, 1000, 1000);

    public static void W(string format, params object[] args)
    {
      lock (msg)
      {
        msg.Enqueue(new Record { args = args, dateTime = DateTime.Now, format = format });
      }
    }
  }
}
