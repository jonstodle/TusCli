using System;
using System.Collections.Generic;
using System.Linq;

namespace TusCli
{
    public class FileInformation
    {
        public string ServerId { get; set; }

        public static FileInformation Parse(string data)
        {
            var values = data.Split(Environment.NewLine)
                .Select(line => line.Split(" = "))
                .ToDictionary(
                    lineParts => lineParts.First(),
                    lineParts => lineParts.Last());

            var info = new FileInformation();
            foreach (var property in typeof(FileInformation).GetProperties())
            {
                property.SetValue(info, values.GetValueOrDefault(property.Name));
            }

            return info;
        }

        public override string ToString() =>
            string.Join(
                Environment.NewLine,
                typeof(FileInformation).GetProperties()
                    .Select(prop => $"{prop.Name} = {prop.GetValue(this)}")
                    .ToArray());
    }
}