"use strict";
var SanJS = window.SanJS || {};

//CRM 
SanJS.WebAPI = SanJS.WebAPI || {};
(function () {
    this.createCRMRecord = function (entitySetName, entity, successCallback, errorCallback) {
        var req = new XMLHttpRequest();
        req.open("POST", encodeURI(this.getWebAPIPath() + entitySetName), false);
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.onreadystatechange = function () {
            if (this.readyState == 4 /* complete */) {
                req.onreadystatechange = null;
                if (this.status == 204) {
                    if (successCallback)
                        successCallback(this.getResponseHeader("OData-EntityId"));
                }
                else {
                    if (errorCallback)
                        errorCallback(SanJS.WebAPI.errorHandler(this.response));
                }
            }
        };
        req.send(JSON.stringify(entity));
    };

    this.getCRMRecordData = function (set, atts, filter) {
        var req = new XMLHttpRequest();
        req.open("GET", encodeURI(this.getWebAPIPath() + set + "?$select=" + atts + "&$filter=" + filter), false);
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.send();

        var recInfo = JSON.parse(req.responseText);
        if (recInfo.value)
            return recInfo.value[0];
        else
            return null;
    }

    this.getClientUrl = function () {
        if (typeof GetGlobalContext == "function" &&
            typeof GetGlobalContext().getClientUrl == "function") {
            return GetGlobalContext().getClientUrl();
        }
        else {
            if (typeof Xrm != "undefined" &&
                typeof Xrm.Page != "undefined" &&
                typeof Xrm.Page.context != "undefined" &&
                typeof Xrm.Page.context.getClientUrl == "function") {
                try {
                    return Xrm.Page.context.getClientUrl();
                } catch (e) {
                    throw new Error("Xrm.Page.context.getClientUrl is not available.");
                }
            }
            else { throw new Error("Context is not available."); }
        }
    }

    this.getWebAPIPath = function () {
        return this.getClientUrl() + "/api/data/v8.2/";
    }

    this.errorHandler = function (resp) {
        try {
            return JSON.parse(resp).error;
        } catch (e) {
            return new Error("Unexpected Error")
        }
    }

    this.getRecordIdFromQueryString = function () {
        var qry = location.search;
        if (qry != "") {
            vals = qry.substr(1).split("&");
            for (var i in vals) {
                vals[i] = vals[i].replace(/\+/g, " ").split("=");
            }
            //look for the parameter named 'id'
            var found = false;
            for (var i in vals) {
                if (vals[i][0].toLowerCase() == "id") {
                    return vals[i][1];
                    break;
                }
            }
        }
        return null;
    }

    this.getCRMRecordsData = function (set, atts, filter) {
        var req = new XMLHttpRequest();
        req.open("GET", encodeURI(this.getWebAPIPath() + set + "?$select=" + atts + "&$filter=" + filter), false);
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.send();

        return JSON.parse(req.responseText).value;
    }

    this.deleteCRMRecord = function (id, set) {
        this.request("DELETE", set + "(" + id + ")");
    }

    this.updateCRMRecord = function (data, id, set) {
        this.request("PATCH", set + "(" + id + ")", data);
    }

    this.updateCRMRecordAttribute = function (data, id, set, attName) {
        this.request("PUT", set + "(" + id + ")/" + attName, data);
    }

    this.request = function (action, uri, data, addHeader) {
        if (!RegExp(action, "g").test("POST PATCH PUT GET DELETE")) { // Expected action verbs.
            throw new Error("Sdk.request: action parameter must be one of the following: " +
                "POST, PATCH, PUT, GET, or DELETE.");
        }
        if (!typeof uri === "string") {
            throw new Error("Sdk.request: uri parameter must be a string.");
        }
        if ((RegExp(action, "g").test("POST PATCH PUT")) && (!data)) {
            throw new Error("Sdk.request: data parameter must not be null for operations that create or modify data.");
        }
        if (addHeader) {
            if (typeof addHeader.header != "string" || typeof addHeader.value != "string") {
                throw new Error("Sdk.request: addHeader parameter must have header and value properties that are strings.");
            }
        }

        uri = this.getWebAPIPath() + uri;

        return new Promise(function (resolve, reject) {
            var request = new XMLHttpRequest();
            request.open(action, encodeURI(uri), true);
            request.setRequestHeader("OData-MaxVersion", "4.0");
            request.setRequestHeader("OData-Version", "4.0");
            request.setRequestHeader("Accept", "application/json");
            request.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            if (addHeader) {
                request.setRequestHeader(addHeader.header, addHeader.value);
            }
            request.onreadystatechange = function () {
                if (this.readyState === 4) {
                    request.onreadystatechange = null;
                    switch (this.status) {
                        case 200: // Operation success with content returned in response body.
                        case 201: // Create success. 
                        case 204: // Operation success with no content returned in response body.
                            resolve(this);
                            break;
                        default: // All other statuses are unexpected so are treated like errors.
                            var error;
                            try {
                                error = JSON.parse(request.response).error;
                            } catch (e) {
                                error = new Error("Unexpected Error");
                            }
                            reject(error);
                            break;
                    }
                }
            };
            request.send(JSON.stringify(data));
        });
    };

}).call(SanJS.WebAPI);