function SyncListMembers(listId) {
    debugger;
    try{
        var baseUrl = window.location.origin + "/api/data/v8.2/";
        var p = { "value": true };

        var request = new XMLHttpRequest();
        request.open("PUT", encodeURI(baseUrl + "lists(" + listId.replace(/[{}]/g, "") + ")/nmc_syncmembers"), true);
        request.setRequestHeader("OData-MaxVersion", "4.0");
        request.setRequestHeader("OData-Version", "4.0");
        request.setRequestHeader("Accept", "application/json");
        request.setRequestHeader("Content-Type", "application/json; charset=utf-8");

        request.send(JSON.stringify(p));
        alert("Started members synchronization to Newsman.");
    }
    catch (err) {
        alert(err.message);
    }
}