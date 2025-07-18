using ieHRMS.Core.Interface; 
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ieHRMS.Core.Config
{
    public class EmailQueueHelper
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IUtilityRepository _utilityRepository;

        public EmailQueueHelper(
            IBackgroundTaskQueue taskQueue,
            IUtilityRepository utilityRepository)
        {
            _taskQueue = taskQueue;
            _utilityRepository = utilityRepository;
        }

        /// <summary>
        /// Queues a single email to be sent in the background with To, CC, and BCC.
        /// </summary>
        public async Task QueueSingleEmailAsync(DataTable smtpTable,string to,string cc,string bcc,string subject,string body)
        {
            await _taskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    string result = await _utilityRepository.SendEmailAsync(smtpTable, to, cc, bcc, subject, body);
                    Console.WriteLine($"✅ Email sent: {result}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to send email: {ex.Message}");
                }
            });
        }
    }
}
