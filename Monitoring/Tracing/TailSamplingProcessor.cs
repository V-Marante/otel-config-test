using System.Diagnostics;
using OpenTelemetry;

namespace cosmos_repository_loggingtests.Monitoring.Tracing
{
    public class TailSamplingProcessor : BaseProcessor<Activity>
    {
        private readonly string _statusTagName;

        public TailSamplingProcessor(string statusTagName = "otel.status_code")
        {
            _statusTagName = statusTagName;
        }

        public override void OnEnd(Activity activity)
        {
            // If the activity is already sampled, we don't need to do anything
            if (activity.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded))
            {
                return;
            }

            // Check if this is an error activity
            var isError = false;

            if (activity.Status == ActivityStatusCode.Error)
            {
                isError = true;
            }
            else if (activity.TagObjects != null)
            {
                foreach (var tag in activity.TagObjects)
                {
                    if (tag.Key == _statusTagName)
                    {
                        if (tag.Value?.ToString() == "ERROR")
                        {
                            isError = true;
                            break;
                        }
                    }
                }
            }

            if (isError)
            {
                Console.WriteLine($"Including error activity with id {activity.Id} and status {activity.Status}");
                activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            }
            else
            {
                Console.WriteLine($"Dropping activity with id {activity.Id} and status {activity.Status}");
            }
        }
    }
}