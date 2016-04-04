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
//using MailAttachment = System.Net.Mail.Attachment;

//using ThreadPool = System.Threading.ThreadPool;
//using WaitCallback = System.Threading.WaitCallback;
//using Encoding = System.Text.Encoding;
//using NetworkCredential = System.Net.NetworkCredential;

namespace Elmah.Gelf
{
    public class ErrorGelfModule : HttpModuleBase, IExceptionFiltering
    {
        private Lazy<IPEndPoint> _lazyIpEndoint;
        private ITransport _transport;


        //[Required]
        public Uri Endpoint { get; set; }

        //public IList<GelfParameterInfo> Parameters { get; private set; }

        public string Facility { get; set; }
        public bool SendLastFormatParameter { get; set; }

        public IGelfConverter Converter { get; private set; }
        //public DnsBase Dns { get; private set; }

        public ErrorGelfModule() : this(new UdpTransport(new UdpTransportClient()), new GelfConverter())
        {
        }

        public ErrorGelfModule(ITransport transport, IGelfConverter converter)
        {
            //Dns = dns;
            _transport = transport;
            Converter = converter;
            //this.Parameters = new List<GelfParameterInfo>();
            _lazyIpEndoint = new Lazy<IPEndPoint>(() =>
            {
                var addresses = Dns.GetHostAddresses(Endpoint.Host);
                var ip = addresses
                    .Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .FirstOrDefault();

                return new IPEndPoint(ip, Endpoint.Port);
            });


            //_lazyITransport = new Lazy<ITransport>(() =>
            //{
            //    return _transports.Single(x => x.Scheme.ToUpper() == Endpoint.Scheme.ToUpper());
            //});
        }






        public event ExceptionFilterEventHandler Filtering;

        /// <summary>
        /// Initializes the module and prepares it to handle requests.
        /// </summary>

        protected override void OnInit(HttpApplication application)
        {
            if (application == null)
                throw new ArgumentNullException("application");

            //
            // Get the configuration section of this module.
            // If it's not there then there is nothing to initialize or do.
            // In this case, the module is as good as mute.
            //

            var config = (IDictionary)GetConfig();

            if (config == null)
                return;

            //
            // Extract the settings.
            //

            //
            // Hook into the Error event of the application.
            //

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

            //if (_reportAsynchronously)
            //    ReportErrorAsync(error);
            //else
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
        /// Schedules the error to be e-mailed asynchronously.
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

            //
            // Catch and trace COM/SmtpException here because this
            // method will be called on a thread pool thread and
            // can either fail silently in 1.x or with a big band in
            // 2.0. For latter, see the following MS KB article for
            // details:
            //
            //     Unhandled exceptions cause ASP.NET-based applications 
            //     to unexpectedly quit in the .NET Framework 2.0
            //     http://support.microsoft.com/kb/911816
            //

            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }

        /// <summary>
        /// Schedules the error to be e-mailed synchronously.
        /// </summary>

        protected virtual void ReportError(Error error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            //
            // Create the mail, setting up the sender and recipient and priority.
            //

            var mail = new MailMessage();
            //
            // Format the mail subject.
            // 
            //
            // Format the mail body.
            //

            var formatter = CreateErrorFormatter();

            var bodyWriter = new StringWriter();
            formatter.Format(bodyWriter, error);
            mail.Body = bodyWriter.ToString();

            switch (formatter.MimeType)
            {
                case "text/html": mail.IsBodyHtml = true; break;
                case "text/plain": mail.IsBodyHtml = false; break;

                default:
                    {
                        throw new ApplicationException(string.Format(
                            "The error mail module does not know how to handle the {1} media type that is created by the {0} formatter.",
                            formatter.GetType().FullName, formatter.MimeType));
                    }
            }

            //var args = new ErrorMailEventArgs(error, mail);

            try
            {
                //
                // If an HTML message was supplied by the web host then attach 
                // it to the mail if not explicitly told not to do so.
                //

                //if (!NoYsod && error.WebHostHtmlMessage.Length > 0)
                //{
                //    var ysodAttachment = CreateHtmlAttachment("YSOD", error.WebHostHtmlMessage);

                //    if (ysodAttachment != null)
                //        mail.Attachments.Add(ysodAttachment);
                //}

                //
                // Send off the mail with some chance to pre- or post-process
                // using event.
                //

                SendMessage(mail);
            }
            finally
            {
                mail.Dispose();
            }
        }

        /// <summary>
        /// Creates the <see cref="ErrorTextFormatter"/> implementation to 
        /// be used to format the body of the e-mail.
        /// </summary>

        protected virtual ErrorTextFormatter CreateErrorFormatter()
        {
            return new ErrorMailHtmlFormatter();
        }

        /// <summary>
        /// Sends the e-mail using SmtpMail or SmtpClient.
        /// </summary>

        protected virtual void SendMessage(MailMessage mail)
        {
            if (mail == null)
                throw new ArgumentNullException("mail");

            //
            // Under .NET Framework 2.0, the authentication settings
            // go on the SmtpClient object rather than mail message
            // so these have to be set up here.
            //

            //using ()

            //var client = new SmtpClient();

            //var host = SmtpServer ?? string.Empty;

            //if (host.Length > 0)
            //{
            //    client.Host = host;
            //    client.DeliveryMethod = SmtpDeliveryMethod.Network;
            //}

            //var port = SmtpPort;
            //if (port > 0)
            //    client.Port = port;

            //var userName = AuthUserName ?? string.Empty;
            //var password = AuthPassword ?? string.Empty;

            //if (userName.Length > 0 && password.Length > 0)
            //    client.Credentials = new NetworkCredential(userName, password);

            //client.EnableSsl = UseSsl;

            //client.Send(mail);
        }

        /// <summary>
        /// Gets the configuration object used by <see cref="OnInit"/> to read
        /// the settings for module.
        /// </summary>

        protected virtual object GetConfig()
        {
            return Configuration.GetSubsection("errorGelf");
        }

        //private static string GetSetting(IDictionary config, string name)
        //{
        //    return GetSetting(config, name, null);
        //}

        //private static string GetSetting(IDictionary config, string name, string defaultValue)
        //{
        //    Debug.Assert(config != null);
        //    Debug.AssertStringNotEmpty(name);

        //    var value = ((string)config[name]) ?? string.Empty;

        //    if (value.Length == 0)
        //    {
        //        if (defaultValue == null)
        //        {
        //            throw new ApplicationException(string.Format(
        //                "The required configuration setting '{0}' is missing for the error mailing module.", name));
        //        }

        //        value = defaultValue;
        //    }

        //    return value;
        //}

    }
}