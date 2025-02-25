using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class Metric
    {
        private Metric()
        {
            this.Values = new LinkedList<int>();
        }

        public Metric(int capacity) : this()
        {
            this.Capacity = capacity;
        }

        public LinkedList<int> Values { get; private set; }

        public int Capacity { get; private set; }

        public int Average(int value)
        {
            this.Append(value);
            return this.Average();
        }

        private int Average()
        {
            return Convert.ToInt32(Math.Ceiling(this.Values.Average()));
        }

        public int Next(int value)
        {
            this.Append(value);
            return this.Next();
        }

        private int Next()
        {
            var values = this.Values.ToArray();
            switch (values.Length)
            {
                case 0:
                    return 0;
                case 1:
                    return values[0];
            }
            var difference = default(double);
            for (var a = 0; a < values.Length - 1; a++)
            {
                var b = values[a];
                var c = values[a + 1];
                difference += c - b;
            }
            difference /= values.Length - 1;
            return Convert.ToInt32(Math.Ceiling(values[values.Length - 1] + difference));
        }

        public virtual void Append(int value)
        {
            if (this.Values.Count >= this.Capacity)
            {
                this.Values.RemoveFirst();
            }
            this.Values.AddLast(value);
        }
    }
}