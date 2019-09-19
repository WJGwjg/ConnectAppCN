using System.Collections.Generic;
using ConnectApp.Models.Model;

namespace ConnectApp.Models.ViewModel {
    public class ChannelMembersScreenViewModel {
        public ChannelView channel;
        public Dictionary<string, bool> followed;
        public List<ChannelMember> members;
        public int nAdmin;
        public bool isLoggedIn;
    }
}