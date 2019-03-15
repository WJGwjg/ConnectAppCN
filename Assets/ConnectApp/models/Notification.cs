using System;

namespace ConnectApp.models {
    [Serializable]
    public class Notification {
        public string id;
        public string userId;
        public string read;
        public string seen;
        public string type;
        public NotificationData data;
        public DateTime createdTime;
        public DateTime updatedTime;
    }
    
    [Serializable]
    public class NotificationData {
        public string id;
        public string fullname;
        public string projectId;
        public string projectTitle;
        public string role;
        public string userId;
        public string username;
    }
}