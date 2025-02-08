namespace CRS.Helpers {
    public class LinkHelper {
        public static string IsActive(string currentAction, string action) {
            return currentAction == action ? "active border-3 border-start border-primary" : string.Empty;
        }

        public static string Show(string currentAction, string action) {
            return currentAction.Contains("Requests") || currentAction.Contains("Send Link") 
                ? "show"
                : string.Empty;
        }
    }
}
