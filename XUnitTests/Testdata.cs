using System;

namespace XUnitTests
{

    [Serializable]
    internal class Testdata
    {
        public int Value { get; set; }

        public Testdata(int v)
        {
            this.Value = v;
        }
    }

}