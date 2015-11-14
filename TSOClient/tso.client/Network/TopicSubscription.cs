using FSO.Common.DataService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Network
{
    public class TopicSubscription : ITopicSubscription
    {
        private ClientDataService DataService;
        private List<ITopic> Topics;

        public TopicSubscription(ClientDataService ds)
        {
            DataService = ds;
        }

        public void Dispose()
        {
            DataService.DiscardTopicSubscription(this);
        }

        public List<ITopic> GetTopics()
        {
            return Topics;
        }

        public void Poll()
        {
            if(Topics != null && Topics.Count > 0){
                DataService.RequestTopics(Topics);
            }
        }

        public void Set(List<ITopic> topics)
        {
            var newTopics = new List<ITopic>();

            if(Topics != null){
                foreach(var item in topics)
                {
                    var existing = Topics.FirstOrDefault(x => x.Equals(item));
                    if(existing == null){
                        newTopics.Add(item);
                    }
                }
            }else{
                newTopics = topics;
            }

            Topics = topics;
            if(newTopics.Count > 0){
                DataService.RequestTopics(newTopics);
            }
        }
    }


    public interface ITopicSubscription : IDisposable
    {
        void Set(List<ITopic> topics);
        void Poll();
    }
}
