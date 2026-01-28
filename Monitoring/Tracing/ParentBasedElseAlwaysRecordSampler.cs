using System.Diagnostics;
using OpenTelemetry.Trace;

namespace cosmos_repository_loggingtests.Monitoring.Tracing
{
    public class ParentBasedElseAlwaysRecordSampler : Sampler
    {
        private readonly Sampler _rootSampler;

        public ParentBasedElseAlwaysRecordSampler(Sampler rootSampler)
            : base()
        {
            _rootSampler = rootSampler ?? throw new ArgumentNullException(nameof(rootSampler));
        }

        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
        {
            // If there's a parent, use its sampling decision
            if (samplingParameters.ParentContext.TraceId != default)
            {
                if (samplingParameters.ParentContext.TraceFlags.HasFlag(ActivityTraceFlags.Recorded))
                {
                    return new SamplingResult(SamplingDecision.RecordAndSample);
                }
                else
                {
                    // Instead of dropping, we record this activity so that we can process it in our
                    // processor
                    return new SamplingResult(SamplingDecision.RecordOnly);
                }
            }

            // This is a root activity. Use the root sampler to make the decision.
            return _rootSampler.ShouldSample(samplingParameters);
        }
    }
}