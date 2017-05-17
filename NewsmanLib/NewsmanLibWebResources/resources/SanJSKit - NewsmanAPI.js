"use strict";
var SanJS = window.SanJS || {};

//NEWSMAN
SanJS.NewsmanAPI = SanJS.NewsmanAPI || {};
(function () {
    this.baseUrl = "https://ssl.newsman.ro/api/1.2/rest/";

    this.retrieveLists = function (uid, ak) {
        var req = new XMLHttpRequest();
        req.open("GET", encodeURI(this.baseUrl + uid + "/" + ak + "/list.all.json"), true);
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        //req.setRequestHeader("Access-Control-Request-Origin", "https://" + window.location.host);
        req.send();

        var recInfo = JSON.parse(req.responseText).d.results[0];
        return recInfo;
    };

    this.retrieveSegments = function (list_id, uid, ak) {
        var req = new XMLHttpRequest();
        req.open("GET", encodeURI(this.baseUrl + uid + "/" + ak + "/segment.all.json?list_id=" + list_id), false);
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.send();

        var recInfo = JSON.parse(req.responseText).d.results[0];
        return recInfo;
    }

    function createSegment(list_id, segment_name, uid, ak) {
        var req = new XMLHttpRequest();
        req.open("POST", encodeURI(this.baseUrl + uid + "/" + ak + "/segment.create.json?list_id=" + list_id + "&segment_name=" + segment_name), true);
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.onreadystatechange = function () {
            if (this.readyState == 4 /* complete */) {
                req.onreadystatechange = null;
                if (this.status == 204) {
                    if (successCallback)
                    { }
                }
                else {
                    errorHandler(resp);
                }
            }
        };
        req.send();
    }

    this.errorHandler = function (resp) {
        try {
            return JSON.parse(resp).error;
        } catch (e) {
            return new Error("Unexpected Error")
        }
    }
}).call(SanJS.NewsmanAPI);