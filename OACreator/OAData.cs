using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OACreator
{
    class OAData
    {
        public int DeNId;
        public string GID;
        public int LP;
        public int AccIdLP;
        public int Strona;
        public int AccIdWym;
        public double Kwota;
        public double KwotaOA;
        public double Procent;

        public OAData(int DeNId, string GID, int LP, int AccIdLP, int Strona, int AccIdWym, double Kwota, double KwotaOA, double Procent)
        {
            this.DeNId = DeNId;
            this.GID = GID;
            this.LP = LP;
            this.AccIdLP = AccIdLP;
            this.Strona = Strona;
            this.AccIdWym = AccIdWym;
            this.Kwota = Kwota;
            this.KwotaOA = KwotaOA;
            this.Procent = Procent;
        }
    }
}
