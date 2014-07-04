using System.Collections.Generic;
using System.Linq;
using OnenoteCapabilities;
using OneNoteObjectModel;

namespace OneNoteMenu
{
    public class AllOneNoteCapabilities
    {
        public readonly OneNoteApp ona = new OneNoteApp();
        public readonly SettingsPeoplePages SettingsPeoplePages = new SettingsPeoplePages();
        public readonly SettingsDailyPages SettingsDailyPages = new SettingsDailyPages();
        public readonly SettingsTopicPages SettingsTopicPages = new SettingsTopicPages();
        public readonly EraseEmpty EraseSections = new EraseEmpty();
        public readonly DailyPages DailyPages;
        public readonly PeoplePages PeoplePages;
        public readonly string[] ListOfPeople;
        public readonly Augmenter Augmenter;

        public AllOneNoteCapabilities ()
        {
            DailyPages = new DailyPages(ona, SettingsDailyPages);
            PeoplePages = new PeoplePages(ona, SettingsPeoplePages);
            ListOfPeople = new SettingsPeoplePages().People().ToArray();
            // TBD: Look up a dependency injection mechanism.
            var smartTagProcessors = new List<ISmartTagProcessor>()
            {
                new TwitterSmartTagProcessor(), 
                new WikipediaSmartTagProcessor(), 
                new PeopleSmartTagProcessor(ona, SettingsPeoplePages), 
                new DailySmartTagProcessor(ona,SettingsDailyPages),
                // Topic smarttag processor needs to go last as it will create a topic page for any un-processed tag.
                new TopicSmartTagTopicProcessor(ona, SettingsTopicPages)
            };
            var smartTagAugmentor = new SmartTagAugmenter(ona, new SettingsSmartTags(), smartTagProcessors);
            Augmenter = new Augmenter(ona, new List<IPageAugmenter> {smartTagAugmentor});
        }
    }
}