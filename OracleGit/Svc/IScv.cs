using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleGit.Svc
{
    interface IScv
    {
        void InitCatalog();
        void AddFile();
        void Commit();
    }
}
