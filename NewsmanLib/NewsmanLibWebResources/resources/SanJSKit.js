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

    this.getCRMRecordsData = function (set, atts, filter) {
        var req = new XMLHttpRequest();
        req.open("GET", encodeURI(this.getWebAPIPath() + set + "?$select=" + atts + "&$filter=" + filter), false);
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.send();

        return JSON.parse(req.responseText).value;
    }

    this.updateCRMRecordAttribute = function (data, id, set, attName) {
        var req = new XMLHttpRequest();
        req.open("PUT", encodeURI(this.getWebAPIPath() + set + "(" + id + ")/" + attName), false);
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.onreadystatechange = function () {
            if (this.readyState == 4 /* complete */) {
                req.onreadystatechange = null;
                switch (this.status) {
                    case 200: // Operation success with content returned in response body.
                    case 201: // Create success. 
                    case 204: // Operation success with no content returned in response body.
                        //alert("Updated successfully!");
                        break;
                    default: // All other statuses are unexpected so are treated like errors.
                        var error;
                        try {
                            error = JSON.parse(req.response).error;
                        } catch (e) {
                            error = new Error("Unexpected Error");
                        }
                        throw error;
                        break;
                }
            }
        };
        req.send(JSON.stringify(data));
    }

    this.displayCustomIcon =  function (rowData, userLCID) {
     var str = JSON.parse(rowData);
     var coldata = str.nmc_action;
     var imgName = "";
     var tooltip = "";
     switch (coldata) {
         case "bounce":
             imgName = "nmc_/icons/bounce.png";
             tooltip = "Bounce";
             break;
         case "send":
             imgName = "nmc_/icons/send.png";
             tooltip = "Send";
             break;
         case "Subscribe":
             imgName = "nmc_/icons/Subscribe.png";
             tooltip = "Subscribe";
             break;
         default:
             imgName = "";
             tooltip = "";
             break;
     }
     var resultarray = [imgName, tooltip];
     return resultarray;
 }

}).call(SanJS.WebAPI);