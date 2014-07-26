﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using OnenoteCapabilities;
using OneNoteObjectModel;

namespace OneNoteObjectModelTests
{
    public class PeoplePagesTests
    {
        public OneNoteApp ona;

        // GRR - I don't understant why I can't open hierarchy, clearly I'm missing something.
        /*
        private static readonly string CurrentAssemblyPath = Path.GetDirectoryName(new Uri(Assembly.GetAssembly(typeof (OneNoteApp)).CodeBase).LocalPath);
        private static readonly string TestNoteBookPath = Path.Combine(CurrentAssemblyPath, @"..\..\..\TestNoteBooks\");
        */
        private TemporaryNoteBookHelper _templateNotebook;
        private TemporaryNoteBookHelper _peoplePagesNotebook;
        private OnenoteCapabilities.SettingsPeoplePages _settingsPeoplePages;
        private PeoplePages peoplePages;

        // Alice already exists and has meetings.
        private readonly string Alice = "Alice";

        // Bob already exists, but has no meetings.
        private readonly string Bob = "Bob";

        // Carl is added at runtime.
        private readonly string Carl = "Carl";

        // Daphne is added at runtime.
        private readonly string Daphne = "Daphne";



        [SetUp]
        public void Setup()
        {
            ona = new OneNoteApp();
            _templateNotebook = new TemporaryNoteBookHelper(ona, "peoplePagesTemplate");
            _peoplePagesNotebook = new TemporaryNoteBookHelper(ona, "peoplePages");

            _settingsPeoplePages = new SettingsPeoplePages()
            {
                    TemplateNotebook = _templateNotebook.Get().name,
                    PeoplePagesNotebook =  _peoplePagesNotebook.Get().name
            };

            // create template structure.
            var templateSection  = ona.CreateSection(_templateNotebook.Get(), _settingsPeoplePages.TemplateSection);
            ona.CreatePage(templateSection, _settingsPeoplePages.TemplatePeopleNextTitle);
            ona.CreatePage(templateSection, _settingsPeoplePages.TemplatePeopleMeetingTitle);

            // create alice 
            var peopleSection = ona.CreateSection(_peoplePagesNotebook.Get(), _settingsPeoplePages.PeoplePagesSection);
            ona.CreatePage(peopleSection, "Parent Week");


            // Create Alice with one meeting a week ago.
            ona.CreatePage(peopleSection, _settingsPeoplePages.PersonNextTitle(Alice));

            var aliceMeeting = ona.CreatePage(peopleSection, _settingsPeoplePages.PersonMeetingTitle(Alice,DateTime.Now - TimeSpan.FromDays(7)));
            aliceMeeting.pageLevel = 2.ToString();
            ona.UpdatePage(aliceMeeting);

            // Create Bob  with no meetings.
            ona.CreatePage(peopleSection, _settingsPeoplePages.PersonNextTitle(Bob));

            // Instantiate peoplePages
            peoplePages = new PeoplePages(ona, _settingsPeoplePages);
        }

        private Page[] PagesForPeopleSection(Func<Page,bool> filter=null)
        {
            var pagesNotebook = _peoplePagesNotebook.Get();
            var pages =  pagesNotebook.PopulatedSection(ona, _settingsPeoplePages.PeoplePagesSection).Page;
            if (filter == null)
            {
                filter = (x) => true;
            }
            return pages.Where(filter).ToArray();
        }

        [Test]
        public void CreateCarl()
        {

            var carlPages = PagesForPeopleSection(p => p.name == _settingsPeoplePages.PersonNextTitle(Carl));
            // Assume Carl doesn't yet exist.
            Assert.That(carlPages,Is.Empty);

            // First going to Carl should create the page.
            peoplePages.GotoPersonNextPage(Carl);

            // Assert  Carl page is created.
            var carlPage = PagesForPeopleSection(p => p.name == _settingsPeoplePages.PersonNextTitle(Carl)).First();
            Assert.That(carlPage.pageLevel, Is.EqualTo(1.ToString()));

            // Now Goto Carl Again, should not create a new carl page.
            peoplePages.GotoPersonNextPage(Carl);

            carlPages = PagesForPeopleSection(p => p.name == _settingsPeoplePages.PersonNextTitle(Carl));
            Assert.That(carlPages.Length, Is.EqualTo(1));

        }
        [Test]
        public void GotoAliceDoesNotCreateANewPage()
        {

            var pagesNotebook = ona.GetNotebook(_peoplePagesNotebook.Get().name);

            // Assume Alice Already Has 1 entry
            Assert.That(PagesForPeopleSection().Count(n => n.name == _settingsPeoplePages.PersonNextTitle(Alice)),Is.EqualTo(1));

            // Now Goto Alice, should not create a new Alice page.
            peoplePages.GotoPersonNextPage(Alice);

            // Assert Alice Already Has 1 entry
            Assert.That(PagesForPeopleSection().Count(n => n.name == _settingsPeoplePages.PersonNextTitle(Alice)),Is.EqualTo(1));
        }

        [Test]
        public void CreateNewAliceMeetingEnsureCreatedInCorrectLocation()
        {

            var pagesNotebook = ona.GetNotebook(_peoplePagesNotebook.Get().name);

            // Assume Alice Already Has 1 next entry.
            Assert.That(PagesForPeopleSection().Count(n => n.name == _settingsPeoplePages.PersonNextTitle(Alice)),Is.EqualTo(1));

            // Assume alice already has 2 entries (next, and one meeting)
            Assert.That(
                PagesForPeopleSection()
                 .SkipWhile(p => p.name != _settingsPeoplePages.PersonNextTitle(Alice))  // find alice.
                 .Skip(1)
                 .TakeWhile(p => p.name.Contains(Alice) && p.pageLevel == "2").Count(),  // get child meetings in sequence.
                 Is.EqualTo(1));

            // Now Goto Alice, should not create a new Alice page.
            peoplePages.GotoPersonCurrentMeetingPage(Alice);

            // Assert Alice now has 3 entries.
            Assert.That(
                PagesForPeopleSection()
                 .SkipWhile(p => p.name != _settingsPeoplePages.PersonNextTitle(Alice))  // find alice.
                 .Skip(1)
                 .TakeWhile(p => p.name.Contains(Alice) && p.pageLevel == "2").Count(),  // count children meetings in sequence.
                 Is.EqualTo(2));
        }

        [Test]
        public void CreateDaphneViaTodayMeeting()
        {

            // Assume Daphne Does Not Exist
            Assert.That(PagesForPeopleSection().Count(n => n.name == _settingsPeoplePages.PersonNextTitle(Daphne)),Is.EqualTo(0));

            // Now Goto Daphne.
            peoplePages.GotoPersonCurrentMeetingPage(Daphne);

            // Assert Daphne has a next meeting page
            Assert.That(PagesForPeopleSection().Count(n => n.name == _settingsPeoplePages.PersonNextTitle(Daphne)),Is.EqualTo(1));

            // Assert Daphne has a current meeting.
            Assert.That(PagesForPeopleSection().Count(n => n.name == _settingsPeoplePages.PersonMeetingTitle(Daphne,DateTime.Now)),Is.EqualTo(1));
        }

        [TearDown]
        public void TearDown()
        {
            _templateNotebook.Dispose();
            _peoplePagesNotebook.Dispose();
        }
    }

}
