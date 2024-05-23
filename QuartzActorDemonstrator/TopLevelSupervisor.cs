using Akka.Actor;
using Akka.Event;
using Akka.Quartz.Actor.Commands;
using Akka.Quartz.Actor.Events;
using Akka.Quartz.Actor;
using Akka.Util.Internal;
using Quartz;
using static QuartzActorDemonstrator.TestActor;
using Microsoft.Extensions.Options;

namespace QuartzActorDemonstrator
{
    internal sealed class TopLevelSupervisor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        string cronSchedule = Program.SchedulerSettingsMonitor?.CurrentValue.CronSchedule ?? " * * * * * ?";
        private readonly Dictionary<string, JobEvent> jobs = [];
        private readonly IActorRef cronActor;
        private readonly IActorRef testActor;

        public TopLevelSupervisor()
        {
            this.cronActor = Context.ActorOf(Props.Create(() => new QuartzActor()), "cron-actor");
            this.testActor = Context.ActorOf(Props.Create(() => new TestActor()), "test-actor");
            
            var testCronTrigger = TriggerBuilder.Create().WithIdentity("TestCronTrigger").WithCronSchedule(this.cronSchedule).Build();
            this.cronActor.Tell(new CreateJob(this.testActor, new TestMessage(), testCronTrigger));

            // cron-actor sends this after it receives CreateJob and finishes setting up a job.
            Receive<JobCreated>(job =>
            {
                logger.Info("Receive {MessageName} {Job}", nameof(JobCreated), job);

                jobs.AddOrSet(job.TriggerKey.Name, job);
            });

            Receive<JobRemoved>(job =>
            {
                logger.Info("Receive {MessageName} {Job}", nameof(JobRemoved), job);
                jobs.Remove(job.TriggerKey.Name);

                this.cronActor.Tell(new CreateJob(this.testActor, new TestMessage(), testCronTrigger));
            });

            Receive<RemoveJobFail>(fail =>
            {
                logger.Info("Receive {MessageName} {Job}", nameof(RemoveJobFail), fail);
            });

            Receive<CreateJobFail>(fail =>
            {
                logger.Info("Receive {MessageName} {Job}", nameof(CreateJobFail), fail);
            });

            Program.SchedulerSettingsMonitor?.OnChange(config =>
            {
                this.logger.Info("{SchedulerSettings} updated.", nameof(SchedulerSettings));

                this.cronSchedule = config.CronSchedule;

                var job = jobs.GetValueOrDefault("TestCronTrigger");

                if (job is not null)
                {
                    // FIXME: job gets removed, but the JobRemoved message goes to DeadLetters instead of sender.
                    this.cronActor.Tell(new RemoveJob(job.JobKey, job.TriggerKey));
                }
            });
        }
    }
}
