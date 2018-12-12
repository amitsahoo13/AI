﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.DeleteEvent.Resources;
using CalendarSkill.Dialogs.Main.Resources;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalendarSkillTest.Flow.Utterances;
using CalendarSkillTest.Flow.Fakes;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Builder;
using CalendarSkill;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class DeleteCalendarFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            this.Services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, IRecognizer>()
                {
                    { "general", new MockLuisRecognizer() },
                    { "calendar", new MockLuisRecognizer(new DeleteMeetingTestUtterances()) }
                }
            });

            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(MockCalendarService.FakeDefaultEvents());
            serviceManager.SetupUserService(MockUserService.FakeDefaultUsers(), MockUserService.FakeDefaultPeople());
        }

        [TestMethod]
        public async Task Test_CalendarDeleteByTitle()
        {
            await this.GetTestFlow()
                .Send(DeleteMeetingTestUtterances.BaseDeleteMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForDeletePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.DeleteEventPrompt())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarDeleteByStartTime()
        {
            DateTime now = DateTime.Now;
            DateTime startTime = new DateTime(now.Year, now.Month, now.Day, 18, 0, 0);
            startTime = startTime.AddDays(1);
            startTime = TimeZoneInfo.ConvertTimeToUtc(startTime);
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(new List<EventModel>
            {
                MockCalendarService.CreateEventModel(
                    startDateTime: startTime,
                    endDateTime: startTime.AddHours(1))
            });
            await this.GetTestFlow()
                .Send(DeleteMeetingTestUtterances.BaseDeleteMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForDeletePrompt())
                .Send("tomorrow 6 pm")
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.DeleteEventPrompt())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarDeleteWithStartTimeEntity()
        {
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            DateTime now = DateTime.Now;
            DateTime startTime = new DateTime(now.Year, now.Month, now.Day, 18, 0, 0);
            startTime = startTime.AddDays(1);
            startTime = TimeZoneInfo.ConvertTimeToUtc(startTime);
            serviceManager.SetupCalendarService(new List<EventModel>()
            {
                MockCalendarService.CreateEventModel(
                    startDateTime: startTime,
                    endDateTime: startTime.AddHours(1))
            });
            await this.GetTestFlow()
                .Send(DeleteMeetingTestUtterances.DeleteMeetingWithStartTime)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.DeleteEventPrompt())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarDeleteWithTitleEntity()
        {
            await this.GetTestFlow()
                .Send(DeleteMeetingTestUtterances.DeleteMeetingWithTitle)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.DeleteEventPrompt())
                .StartTestAsync();
        }

        private string[] AskForDeletePrompt()
        {
            return this.ParseReplies(DeleteEventResponses.NoDeleteStartTime.Replies, new StringDictionary());
        }

        private string[] DeleteEventPrompt()
        {
            return this.ParseReplies(DeleteEventResponses.EventDeleted.Replies, new StringDictionary());
        }

        private Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
            };
        }

        private Action<IActivity> ShowCalendarList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }
    }
}
