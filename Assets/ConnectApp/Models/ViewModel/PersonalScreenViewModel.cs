using System.Collections.Generic;
using ConnectApp.Models.Model;

namespace ConnectApp.Models.ViewModel {
    public class PersonalScreenViewModel {
        public bool isLoggedIn;
        public LoginInfo user;
        public Dictionary<string, User> userDict;
        public bool scanEnabled;
    }
}