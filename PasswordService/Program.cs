using System;
using CommandLine;
using Topshelf;

namespace PasswordService
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                  .WithParsed(opts =>
                              {
                                  if (!string.IsNullOrWhiteSpace(opts.Password))
                                  {
                                      var encrypted = Password.Set(opts.Password);
                                      Console.WriteLine($"Password has been encrypted and saved: '{encrypted}'");
                                  }

                                  if (opts.NoService) Environment.Exit((int)TopshelfExitCode.Ok);
                              });
            try
            {
                var host = HostFactory.New(x =>
                                           {
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

        #region Nested type: Options

        internal class Options
        {
            [Option('n', "noservice", Required = false, Default = false, HelpText = "Do not run the service.")]
            public bool NoService { get; set; }

            [Option('p', "setpassword", Required = false, HelpText = "Encrypts the provided plain-text password and saves it to the app.config")]
            public string Password { get; set; }
        }

        #endregion
    }
}