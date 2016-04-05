using System;
using System.Diagnostics;
using System.Globalization;
using System.Web;
using System.IO;
using System.Net.Mail;
using System.Collections;
using System.Collections.Generic;
using Elmah.Gelf.Transport;
using Elmah.Gelf.Converters;
using System.Net;
using System.Linq;
using System.Configuration;


namespace Elmah.Gelf
{
    public class ErrorGelfModule : HttpModuleBase, IExceptionFiltering
    {
        private IPEndPoint _ipEndoint;
        private ITransport _transport;
        private string _facility;
        private bool _reportAsynchronously;
        private Uri _endpoint;


        /// <summary>
        /// Ignored additional properties, if it not necessary
        /// </summary>
        ICollection<string> _ignoredProperties;


        public IGelfConverter Converter { get; private set; }

        public ErrorGelfModule() : this(new UdpTransport(new UdpTransportClient()), new GelfConverter())
        {
        }

        public ErrorGelfModule(ITransport transport, IGelfConverter converter)
        {
            _transport = transport;
            Converter = converter;

            _ignoredProperties = new List<string>();

        }

        public event ExceptionFilterEventHandler Filtering;

        /// <summary>
        /// Initializes the module and prepares it to handle requests.
        /// </summary>

        protected override void OnInit(HttpApplication application)
        {
            if (application == null)
                throw new ArgumentNullException("application");

            var config = (IDictionary)GetConfig();

            if (config == null)
                return;

            #region Settings
            // Extract the settings.
            _facility = GetSetting(config, "facility");
            _reportAsynchronously = Convert.ToBoolean(GetSetting(config, "async", bool.TrueString));
            var ignored = GetSetting(config, "ignored", string.Empty);
            if (!string.IsNullOrEmpty(ignored)) _ignoredProperties = ignored.Split(';');

            if (!Uri.TryCreate(GetSetting(config, "endpoint"), UriKind.RelativeOrAbsolute, out _endpoint))
                throw new ArgumentException("endpoint");

            var addresses = Dns.GetHostAddresses(_endpoint.Host);
            var ip = addresses
                .Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .FirstOrDefault();

            _ipEndoint = new IPEndPoint(ip, _endpoint.Port);
            #endregion


            application.Error += OnError;
            ErrorSignal.Get(application).Raised += OnErrorSignaled;

        }

        /// <summary>
        /// The handler called when an unhandled exception bubbles up to 
        /// the module.
        /// </summary>
        protected virtual void OnError(object sender, EventArgs e)
        {
            var context = ((HttpApplication)sender).Context;
            OnError(context.Server.GetLastError(), context);
        }

        /// <summary>
        /// The handler called when an exception is explicitly signaled.
        /// </summary>
        protected virtual void OnErrorSignaled(object sender, ErrorSignalEventArgs args)
        {
            OnError(args.Exception, args.Context);
        }

        /// <summary>
        /// Reports the exception.
        /// </summary>
        protected virtual void OnError(Exception e, HttpContext context)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            //
            // Fire an event to check if listeners want to filter out
            // reporting of the uncaught exception.
            //

            var args = new ExceptionFilterEventArgs(e, context);
            OnFiltering(args);

            if (args.Dismissed)
                return;

            //
            // Get the last error and then report it synchronously or 
            // asynchronously based on the configuration.
            //

            var error = new Error(e, context);

            if (_reportAsynchronously)
                ReportErrorAsync(error);
            else
                ReportError(error);
        }

        /// <summary>
        /// Raises the <see cref="Filtering"/> event.
        /// </summary>
        protected virtual void OnFiltering(ExceptionFilterEventArgs args)
        {
            var handler = Filtering;

            if (handler != null)
                handler(this, args);
        }

        /// <summary>
        /// Schedules the error to be sent asynchronously.
        /// </summary>
        /// <remarks>
        /// The default implementation uses the <see cref="ThreadPool"/>
        /// to queue the reporting.
        /// </remarks>

        protected virtual void ReportErrorAsync(Error error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            System.Threading.ThreadPool.QueueUserWorkItem(ReportError, error);
        }

        private void ReportError(object state)
        {
            try
            {
                ReportError((Error)state);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }

        /// <summary>
        /// Schedules the error to be sent synchronously.
        /// </summary>

        protected virtual void ReportError(Error error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            try
            {
                var jsonString = Converter.GetGelfJson(error, _facility, _ignoredProperties);

                if (jsonString == null) return;

                _transport.Send(_ipEndoint, jsonString);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }

        #region Config
        /// <summary>
        /// Gets the configuration object used by <see cref="OnInit"/> to read
        /// the settings for module.
        /// </summary>
        protected virtual object GetConfig()
        {
            return Configuration.GetSubsection("errorGelf");
        }

        private static string GetSetting(IDictionary config, string name)
        {
            return GetSetting(config, name, null);
        }

        private static string GetSetting(IDictionary config, string name, string defaultValue)
        {
            Debug.Assert(config != null);

            var value = ((string)config[name]) ?? string.Empty;

            if (value.Length == 0)
            {
                if (defaultValue == null)
                {
                    throw new ApplicationException(string.Format("The required configuration setting '{0}' is missing for the error mailing module.", name));
                }

                value = defaultValue;
            }

            return value;
        }
        #endregion

    }
}