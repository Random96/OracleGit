using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleGit.Database
{
    internal class DBObject
    {
        public string Name { get; set; }
        public string Ext { get; set; }

        public static DBObject[] dbObjects = new DBObject[]
        {
            new DBObject() { Name = "PROCEDURE", Ext = ".prc" },
            new DBObject() { Name = "FUNCTION", Ext = ".FNC" },
            new DBObject() { Name = "PACKAGE", Ext = ".SPC" },
            new DBObject() { Name = "PACKAGE BODY", Ext = ".BDY" },
            new DBObject() { Name = "TRIGGER", Ext = ".TRG" },
            new DBObject() { Name = "VIEW", Ext = ".SQL" },
            new DBObject() { Name = "TABLE", Ext = ".SQL" },
        };
    }
}