using System;
using Topshelf;

namespace PasswordService
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                var host = HostFactory.New(x =>
                                           {
                                               x.AddCommandLineDefinition("setpassword", password => 
                                               {
                                                   var encrypted = Password.Set(password);
                                                   Console.WriteLine($"Password has been encrypted and saved: '{encrypted}'");
                                               });
                                               x.AddCommandLineSwitch("noservice", _ => { Environment.Exit((int)TopshelfExitCode.Ok); });

                                               x.Service<CodeSignerPasswordService>(s =>
                                                                                    {
                                                                                        s.ConstructUsing(factory => new CodeSignerPasswordService());
                                                                                        s.WhenStarted(tc => tc.Start());
                                                                                        s.WhenStopped(tc => tc.Stop());
                                                                                        s.WhenPaused(tc => tc.Pause());
                                                                                        s.WhenContinued(tc => tc.Continue());
                                                                                        s.WhenShutdown(tc => tc?.Dispose());
                                                                                    });
                                               x.RunAsLocalSystem();
                                               x.StartAutomatically();
                                               x.EnableServiceRecovery(r =>
                                                                       {
                                                                           r.RestartService(1);
                                                                           r.SetResetPeriod(0);
                                                                       });
                                               x.EnablePauseAndContinue();
                                               x.EnableShutdown();
                                               x.SetDescription("Automates password entry for the SafeNet CodeSigner app");
                                               x.SetDisplayName("SafeNet Password Monitor");
                                               x.SetServiceName("SafeNetPasswordMonitor");
                                               x.EnableShutdown();
                                           });

                var exitCode = host.Run();
                return exitCode == TopshelfExitCode.Ok ? 0 : (int)exitCode;
            }
            catch (Exception)
            {
                return (int)TopshelfExitCode.AbnormalExit;
            }
        }
    }
}