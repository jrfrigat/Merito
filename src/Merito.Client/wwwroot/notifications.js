window.meritoNotifications = {
    getPermission: () => {
        if (!("Notification" in window)) return "unsupported";
        return Notification.permission;
    },
    requestPermission: async () => {
        if (!("Notification" in window)) return "unsupported";
        return await Notification.requestPermission();
    },
    show: (title, message) => {
        if (!("Notification" in window) || Notification.permission !== "granted") return;
        new Notification(title, { body: message, icon: "icon-192.png" });
    }
};
