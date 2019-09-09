using System;
using System.Configuration;
using System.Diagnostics;
using System.Security;
using System.Windows.Automation;

namespace PasswordService
{
    internal class CodeSignerProgramSettings
    {
        private readonly EventLog _log;

        public CodeSignerProgramSettings() { }

        public CodeSignerProgramSettings(EventLog log) { _log = log; }

        public string AcceptButtonContent { get; set; } = "OK";

        public string PasswordBoxName { get; set; } = "Token Password:";

        public string ProgramName { get; set; } = "Token Logon";

        public void PopulateFromAppSettings()
        {
            try
            {
                var val = ConfigurationManager.AppSettings["ProgramName"];
                if (!string.IsNullOrWhiteSpace(val)) ProgramName = val;
            }
            catch (ConfigurationErrorsException ex) { _log?.WriteEntry(ex.Message, EventLogEntryType.Error); }

            try
            {
                var val = ConfigurationManager.AppSettings["PasswordBoxName"];
                if (!string.IsNullOrWhiteSpace(val)) PasswordBoxName = val;
            }
            catch (ConfigurationErrorsException ex) { _log?.WriteEntry(ex.Message, EventLogEntryType.Error); }

            try
            {
                var val = ConfigurationManager.AppSettings["OkButtonContent"];
                if (!string.IsNullOrWhiteSpace(val)) AcceptButtonContent = val;
            }
            catch (ConfigurationErrorsException ex) { _log?.WriteEntry(ex.Message, EventLogEntryType.Error); }
        }
    }

    public class CodeSignerPasswordService : IDisposable
    {
        private readonly CodeSignerProgramSettings _codeSignerSettings;

        private readonly EventLog _log;

        private SecureString _storedPassword;

        public CodeSignerPasswordService()
        {
            const string logSource = nameof(CodeSignerPasswordService);
            var logName = $"{nameof(CodeSignerPasswordService)}Log";
            try
            {
                if (!EventLog.SourceExists(nameof(CodeSignerPasswordService))) EventLog.CreateEventSource(logSource, logName);
                _log = new EventLog
                       {
                           Source = logSource,
                           Log = logName
                       };
            }catch(Exception ex) { }

            _codeSignerSettings = new CodeSignerProgramSettings(_log);

            try
            {
                _storedPassword = Password.Get();
            }
            catch (ArgumentException ex)
            {
                _log?.WriteEntry(ex.Message, EventLogEntryType.Error);
                throw;
            }
        }

        #region IDisposable

        public void Dispose()
        {
            Unsubscribe();
            _log?.Dispose();
            _storedPassword.Dispose();
        }

        #endregion

        public void Continue()
        {
            _log?.WriteEntry("CodeSignerPasswordService continuing...", EventLogEntryType.Information);
            Subscribe();
        }

        private void OnWindowOpened(object sender, AutomationEventArgs e)
        {
            var element = sender as AutomationElement;
            if ((element == null) || !string.Equals(element.Current.Name, _codeSignerSettings.ProgramName, StringComparison.Ordinal)) return;

            var pattern = (WindowPattern)element.GetCurrentPattern(WindowPattern.Pattern);
            pattern.WaitForInputIdle(10000);
            var edit = element.FindFirst(TreeScope.Descendants, new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit), new PropertyCondition(AutomationElement.NameProperty, _codeSignerSettings.PasswordBoxName)));
            var ok = element.FindFirst(TreeScope.Descendants, new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button), new PropertyCondition(AutomationElement.NameProperty, _codeSignerSettings.AcceptButtonContent)));
            if ((edit != null) && (ok != null))
            {
                var vp = (ValuePattern)edit.GetCurrentPattern(ValuePattern.Pattern);
                vp.SetValue(_storedPassword.ToInsecureString());
                var ip = (InvokePattern)ok.GetCurrentPattern(InvokePattern.Pattern);
                ip.Invoke();
            }
            else
            {
                _log?.WriteEntry("CodeSignerPasswordService stopping...", EventLogEntryType.Information);
                Console.WriteLine("SafeNet window detected but could not find password box and/or 'OK' button");
            }
        }

        public void Pause()
        {
            _log?.WriteEntry("CodeSignerPasswordService pausing...", EventLogEntryType.Information);
            Unsubscribe();
        }

        public void Start()
        {
            _log?.WriteEntry("CodeSignerPasswordService starting...", EventLogEntryType.Information);
            Subscribe();
            _storedPassword = new SecureString();
        }

        public void Stop()
        {
            _log?.WriteEntry("CodeSignerPasswordService stopping...", EventLogEntryType.Information);
            Unsubscribe();
        }

        private void Subscribe() { Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent, AutomationElement.RootElement, TreeScope.Children, OnWindowOpened); }

        private void Unsubscribe() { Automation.RemoveAllEventHandlers(); }
    }
}