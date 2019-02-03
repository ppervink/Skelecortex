using System;
using System.Threading.Tasks;

namespace Skelecortex.Plumbing
{
    public sealed class PipeConnector<TContent>
    {
        public PipeConnector (IPipeOutput<TContent> input, IPipeInput<TContent> output)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public async Task Flow (IFlowController flowController)
        {
            if (flowController is null)
                throw new ArgumentNullException(nameof(flowController));

            while (flowController.IsFlowing)
            {
                var flow = await Input.Flow(flowController);
                var allowFlow = flow.IsFlowing;
                if (flowController.IsFlowing)
                {
                    allowFlow = flowController.Flow(flow) && allowFlow;
                }
                else
                {
                    allowFlow = false;
                }

                if (allowFlow)
                {
                    await Output.Flow(flow.Content, flowController);
                }
            }
        }

        public IPipeOutput<TContent> Input { get; }

        public IPipeInput<TContent> Output { get; }
    }
}
