using Oracle.ManagedDataAccess.Client;
using OracleGit.Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleGit
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length >= 4)
            {
                var db = new Database.Database() { User = args[0], Password = args[1], Server = args[2], URL = args[3] };
                UploadServer(db);
            }
        }

        static string BAD_EOL = "\r";

        private static void UploadServer(Database.Database Server)
        {
            try
            {
                List<DBObject> Obects = new List<DBObject>()
                {
                    new DBObject() { Name = "PROCEDURE", Ext = ".prc" },
                    new DBObject() { Name = "FUNCTION", Ext = ".FNC" },
                    new DBObject() { Name = "PACKAGE", Ext = ".SPC" },
                    new DBObject() { Name = "PACKAGE BODY", Ext = ".BDY" },
                    new DBObject() { Name = "TRIGGER", Ext = ".TRG" },
                    new DBObject() { Name = "VIEW", Ext = ".SQL" },
                    new DBObject() { Name = "TABLE", Ext = ".SQL" },
                };

                using (OracleConnection cn = new OracleConnection("User Id=" + Server.User + "; Password=" + Server.Password + "; Data Source=" + Server.Server + ";"))
                {
                    try
                    {
                        cn.Open();
                    }
                    catch (OracleException)
                    {
                        Console.WriteLine($"Oracle connection error with server {Server.User}\n");
                        return;
                    }

                    string TmpDir = Path.GetTempPath();

                    if (TmpDir.Last() != '\\')
                        TmpDir += '\\';

                    TmpDir += Server.Server;

                    if (System.IO.Directory.Exists(TmpDir))
                        System.IO.Directory.Delete(TmpDir, true);

                    System.IO.Directory.CreateDirectory(TmpDir);
                    try
                    {
                        using (Process myProcess = new Process())
                        {
                            myProcess.StartInfo.FileName = "git";
                            myProcess.StartInfo.CreateNoWindow = true;
                            myProcess.StartInfo.UseShellExecute = false;
                            myProcess.StartInfo.RedirectStandardInput = true;
                            myProcess.StartInfo.RedirectStandardOutput = true;
                            myProcess.StartInfo.RedirectStandardError = true;

                            myProcess.StartInfo.WorkingDirectory = TmpDir;

                            myProcess.StartInfo.Arguments = $"clone {Server.URL} {TmpDir}";
                            // myProcess.StartInfo.Arguments = "co " + Server.URL + " " + TmpDir + " --force -q";
#if DEBUG
                            Console.WriteLine($"check {Server.Server} begin");
#endif
                            myProcess.Start();
#if DEBUG
                            string sError = myProcess.StandardError.ReadToEnd();
                            if (string.IsNullOrWhiteSpace(sError))
                                Console.WriteLine("check out compleate");
                            else
                                Console.WriteLine($"Error: {sError}");
#endif
                            myProcess.WaitForExit();

                            // OracleCommand CmdParse = new OracleCommand("SELECT DBMS_METADATA.GET_DDL( :OBJECT_TYPE, :NAME) FROM DUAL", cn);
                            using (OracleCommand CmdParse = new OracleCommand("SELECT TEXT FROM USER_SOURCE WHERE TYPE = :OBJECT_TYPE AND NAME = :NAME ORDER BY LINE", cn))
                            {
                                OracleParameter ObjectType = CmdParse.Parameters.Add("OBJECT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input);
                                OracleParameter NameProc = CmdParse.Parameters.Add("NAME", OracleDbType.Varchar2, ParameterDirection.Input);

                                using (OracleCommand Cmd = new OracleCommand("SELECT OBJECT_NAME FROM USER_OBJECTS WHERE OBJECT_TYPE = :B1 /* AND OBJECT_NAME = 'IN_QQ' */ ORDER BY OBJECT_NAME", cn))
                                {
                                    OracleParameter ObjectTypeName = Cmd.Parameters.Add("B1", OracleDbType.Varchar2, ParameterDirection.Input);

                                    using (OracleCommand Describe = new OracleCommand("SELECT DBMS_METADATA.GET_DDL( :B1, :B2) FROM DUAL", cn))
                                    {
                                        OracleParameter dType = Describe.Parameters.Add("B1", OracleDbType.Varchar2, ParameterDirection.Input);
                                        OracleParameter dName = Describe.Parameters.Add("B2", OracleDbType.Varchar2, ParameterDirection.Input);

                                        foreach (DBObject dbObj in Obects)
                                        {
#if DEBUG
                                            Console.WriteLine(dbObj.Name);
#endif
                                            ObjectType.Value = dbObj.Name;
                                            ObjectTypeName.Value = dbObj.Name;

                                            using (OracleDataReader reader = Cmd.ExecuteReader())
                                            {
                                                while (reader.Read())
                                                {
                                                    NameProc.Value = reader[0];
                                                    string fileName = TmpDir + '\\' + reader[0] + dbObj.Ext;

                                                    StreamWriter outfile = null;

                                                    using (OracleDataReader ProcReader = CmdParse.ExecuteReader())
                                                    {
                                                        while (ProcReader.Read())
                                                        {
                                                            if (outfile == null)
                                                            {
                                                                FileStream fs = new FileStream(fileName, FileMode.Create);
                                                                outfile = new StreamWriter(fs, Encoding.GetEncoding(1251), 512, false);
                                                            }
                                                            string out_str = ProcReader[0].ToString();


                                                            while (out_str.Length > 0 && (out_str.Last() == 10 || out_str.Last() == 13))
                                                                out_str = out_str.Substring(0, out_str.Length - 1);


                                                            outfile.WriteLine(out_str);
                                                        }

                                                        ProcReader.Close();
                                                    }

                                                    if (outfile == null)
                                                    {
                                                        dType.Value = dbObj.Name;
                                                        dName.Value = reader[0];

                                                        try
                                                        {
                                                            var Out = Describe.ExecuteScalar();
                                                            FileStream fs = new FileStream(fileName, FileMode.Create);
                                                            outfile = new StreamWriter(fs, Encoding.GetEncoding(1251), 512, false);
                                                            outfile.Write(Out.ToString().Replace(BAD_EOL, "\r\n"));
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            System.Console.WriteLine(ex.ToString());
                                                        }
                                                    }

                                                    if (outfile != null)
                                                    {
                                                        outfile.Flush();
                                                        outfile.Close();

                                                        myProcess.StartInfo.Arguments = "status -s " + fileName;
                                                        myProcess.Start();
                                                        string text_cmd = myProcess.StandardOutput.ReadToEnd();
                                                        myProcess.WaitForExit();

                                                        if (text_cmd.Length > 0 && text_cmd[0] == '?')
                                                        {
                                                            myProcess.StartInfo.Arguments = "add " + fileName;
                                                            myProcess.Start();
#if DEBUG
                                                            sError = myProcess.StandardError.ReadToEnd();
                                                            if (!string.IsNullOrWhiteSpace(sError))
                                                                Console.WriteLine(sError);
#endif
                                                            myProcess.WaitForExit();
                                                        }

                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                myProcess.StartInfo.Arguments = $"commit -a -m \"daily commit on {DateTime.Now}\"";
                                myProcess.Start();
#if DEBUG
                                Console.WriteLine(myProcess.StandardOutput.ReadToEnd());
                                Console.WriteLine(myProcess.StandardError.ReadToEnd());
#endif
                                myProcess.WaitForExit();

                                myProcess.StartInfo.Arguments = $"push";
                                myProcess.Start();
#if DEBUG
                                Console.WriteLine(myProcess.StandardOutput.ReadToEnd());
                                Console.WriteLine(myProcess.StandardError.ReadToEnd());
#endif
                                myProcess.WaitForExit();
                            }
                        }
                    }
                    finally
                    {
                        System.IO.Directory.Delete(TmpDir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}