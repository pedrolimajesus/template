using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using AppComponents;
using AppComponents.ControlFlow;
using AppComponents.Raven;
using LoK.ManagedApp;
using Lok.Unik.ModelCommon.Client;

namespace Shrike.DAL.Manager
{
    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class SchedulePlanManager
    {
        //private readonly TagBusinessLogic _tagBusinessLogic;

        public SchedulePlanManager()
        {
            //_tagBusinessLogic = new TagBusinessLogic();
        }

        public IEnumerable<Lok.Unik.ModelCommon.Client.SchedulePlan> GetAll()
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var q2 = from schedule in session.Query<Lok.Unik.ModelCommon.Client.SchedulePlan>() select schedule;
                    return q2.ToArray();
                }
            }
        }

        public Lok.Unik.ModelCommon.Client.SchedulePlan GetSchedulePlanById(Guid Id)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var query = from schedule in session.Query<Lok.Unik.ModelCommon.Client.SchedulePlan>()
                                where schedule.Id == Id
                                select schedule;
                    return query.FirstOrDefault();
                }
            }
        }

        public Lok.Unik.ModelCommon.Client.SchedulePlan GetSchedulePlanByIdModel(Guid id)
        {
            var schedule = GetSchedulePlanById(id);

            return schedule;
        }

        public void SaveTimeLineSchedule(Guid id, TimeLineSchedule timeLine)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var currentScheduleList = from schedule in session.Query<Lok.Unik.ModelCommon.Client.SchedulePlan>()
                                              where schedule.Id == id
                                              select schedule;
                    var currentSchedule = currentScheduleList.FirstOrDefault();
                    if (currentSchedule != null) currentSchedule.TimeLineSchedules.Add(timeLine);

                    session.SaveChanges();
                }
            }
        }

        public void UpdateScheduleTag(SchedulePlan schedulePlan)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var currentSchedule = session.Load<SchedulePlan>(schedulePlan.Id);
                    currentSchedule.Tags = schedulePlan.Tags;
                    session.SaveChanges();
                }
            }
        }
        public void UpdateScheduleDTO(SchedulePlan schedulePlan)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var currentSchedule = session.Load<SchedulePlan>(schedulePlan.Id);
                    currentSchedule.Name = schedulePlan.Name;
                    currentSchedule.TimeLineSchedules = schedulePlan.TimeLineSchedules;
                    session.SaveChanges();
                }
            }
        }

        public void UpdateNameSchedulePlanDto(SchedulePlan schedulePlan)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var currentSchedule = session.Load<SchedulePlan>(schedulePlan.Id);
                    currentSchedule.Name = schedulePlan.Name;
                    session.SaveChanges();
                }
            }
        }
        public void EnableDisableSchedule(Guid id, string status)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var currentSchedule = session.Load<SchedulePlan>(id);
                    if (status.Equals("Enabled"))
                    {
                        currentSchedule.Status = SchedulePlanStatus.Active;
                    }
                    else
                    {
                        currentSchedule.Status = SchedulePlanStatus.Disable;
                    }
                    session.SaveChanges();
                }
            }
        }

        public void DeleteSchedulePlan(Guid id)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var currentSchedule = session.Load<SchedulePlan>(id);
                    currentSchedule.Status = SchedulePlanStatus.InActive;
                    session.SaveChanges();
                }
            }
        }

        public void DeleteTimeLineSchedule(Guid id, Guid idtimeLineSchedule, string dayName)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var currentSchedule = session.Load<SchedulePlan>(id);
                    foreach (var timeLineSchedule in currentSchedule.TimeLineSchedules)
                    {
                        if (timeLineSchedule.Id.Equals(idtimeLineSchedule))
                        {
                            if (dayName.Length > 0)
                            {
                                Day dayRemoved = new Lok.Unik.ModelCommon.Client.Day();

                                if (timeLineSchedule.DaysOfWeek.Count > 1)
                                {
                                    foreach (Day day in timeLineSchedule.DaysOfWeek)
                                    {
                                        if (day.DayOfWeek.ToString() == dayName)
                                        {
                                            dayRemoved = day;
                                            break;
                                        }
                                    }
                                    timeLineSchedule.DaysOfWeek.Remove(dayRemoved);
                                }
                                else
                                {
                                    currentSchedule.TimeLineSchedules.Remove(timeLineSchedule);
                                }
                            }
                            else
                                currentSchedule.TimeLineSchedules.Remove(timeLineSchedule);

                            break;
                        }
                    }
                    session.SaveChanges();
                }
            }
        }

        public IEnumerable<SchedulePlan> GetAllSchedulePlan()
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var q2 = from entity in session.Query<Lok.Unik.ModelCommon.Client.SchedulePlan>() select entity;

                    return q2.ToList();
                }
            }
        }

        public Lok.Unik.ModelCommon.Client.SchedulePlan SaveSchedule(SchedulePlan schedule)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    schedule.Tags.Add(new TagManager().AddDefault<SchedulePlan>(schedule.Name, schedule.Id.ToString()));

                    session.Store(schedule);
                    session.SaveChanges();

                    return schedule;
                }
            }
        }

        //public List<LayoutTemplate> LayoutTemplates()
        //{

        //    var layoutTemplates = new List<LayoutTemplate>();
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.Full), SrcImage = "../../../Content/images/PlacementLayout/1L.png" });
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.Horizontal1_2), SrcImage = "../../../Content/images/PlacementLayout/2L.png" });
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.Horizontal2_1), SrcImage = "../../../Content/images/PlacementLayout/3L.png" });
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.HorizontalHalves), SrcImage = "../../../Content/images/PlacementLayout/4L.png" });
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.HorizontalSideBurns), SrcImage = "../../../Content/images/PlacementLayout/5L.png" });
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.HorizontalThirds), SrcImage = "../../../Content/images/PlacementLayout/6L.png" });
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.PantsHandstand), SrcImage = "../../../Content/images/PlacementLayout/7L.png" });
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.PantsLeft), SrcImage = "../../../Content/images/PlacementLayout/8L.png" });
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.PantsRight), SrcImage = "../../../Content/images/PlacementLayout/9L.png" });
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.PantsStand), SrcImage = "../../../Content/images/PlacementLayout/10L.png" });
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.Vertical1_2), SrcImage = "../../../Content/images/PlacementLayout/11L.png" });
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.Vertical2_1), SrcImage = "../../../Content/images/PlacementLayout/12L.png" });
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.VerticalHalves), SrcImage = "../../../Content/images/PlacementLayout/13L.png" });
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.VerticalSandwich), SrcImage = "../../../Content/images/PlacementLayout/14L.png" });
        //    layoutTemplates.Add(new LayoutTemplate() { Id = Guid.NewGuid(), StandardViewPorts = StandardPlacementLayoutSpecification.DescribeLayout(StandardPlacementLayouts.VerticalThirds), SrcImage = "../../../Content/images/PlacementLayout/15L.png" });

        //    return layoutTemplates;
        //}

        //private IEnumerable<SchedulePlanDTO> FromScheduleModel(IEnumerable<Lok.Unik.ModelCommon.Client.SchedulePlan> schedulePlan)
        //{
        //    return schedulePlan.Select(schedule => new SchedulePlanDTO
        //    {
        //        Id = schedule.Id,
        //        Name = schedule.Name,
        //        DeviceCount = "0",
        //        NextSchedule = "0",
        //        TimeLineScheduleDatas = new Collection<TimeLineSchedule>(),
        //        Tags = (ICollection<Tag>)_tagBusinessLogic.ChangeCommonTagAModelTag(schedule.Tags),
        //    }).ToList();
        //}
    }
}
