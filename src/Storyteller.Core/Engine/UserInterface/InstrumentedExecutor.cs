using Storyteller.Core.Engine.Batching;
using Storyteller.Core.Grammars;
using Storyteller.Core.Messages;

namespace Storyteller.Core.Engine.UserInterface
{
    public class InstrumentedExecutor : SynchronousExecutor
    {
        private readonly IUserInterfaceObserver _observer;
        private readonly int _total;
        private int _step;
        private readonly SpecificationPlan _plan;

        public InstrumentedExecutor(ISpecContext context, SpecificationPlan plan, IUserInterfaceObserver observer) : base(context)
        {
            _observer = observer;
            _total = plan.Count();
            _step = 0;
            _plan = plan;
        }

        public override void Line(ILineExecution execution)
        {
            base.Line(execution);

            var progress = new SpecProgress(_plan.Specification.Id, CurrentContext.Counts.Clone(), ++_step, _total);
            _observer.SendProgress(progress);
        }
    }
}