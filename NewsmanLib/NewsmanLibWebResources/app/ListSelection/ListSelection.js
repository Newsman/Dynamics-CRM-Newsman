var viewModel;

$(document).ready(function () {
    viewModel = new NMConfigViewModel();
    ko.applyBindings(viewModel);
});

var NMConfigViewModel = function () {
    var self = this;

    self.nmApiKey = ko.observable();
    self.nmApiKeyLoaded = ko.observable(false);
    self.nmUserId = ko.observable();
    self.nmUserIdLoaded = ko.observable(false);
    self.nmLists = ko.observableArray([]);
    self.nmSegments = ko.observableArray([]);
    self.nmSelectedList = ko.observable();
    self.nmSelectedSegment = ko.observable();
    self.configSaved = ko.observable(self.nmApiKeyLoaded() && self.nmUserIdLoaded());
    self.listsLoaded = ko.computed(function () {
        return self.nmLists() && self.nmLists()[0] && self.nmLists()[0].list_id;
    });

    self.init = function () {
        //get CRM info
        var recordInfo = retrieveCRMConfiguration();
        if (recordInfo.akConfig) {
            self.nmApiKey = ko.observable(recordInfo.akConfig);
            self.nmApiKeyLoaded = ko.observable(true);
        }
        if (recordInfo.uidConfig) {
            self.nmUserId = ko.observable(recordInfo.uidConfig);
            self.nmUserIdLoaded = ko.observable(true);
        }

        self.configSaved(self.nmApiKeyLoaded() && self.nmUserIdLoaded());
        self.nmLoadLists();
    };

    self.nmLoadLists = function () {
        if (self.configSaved()) {
            var nmLists = SanJS.WebAPI.getCRMRecordData("nmc_newsmanconfigs", "nmc_value", "nmc_name eq 'Newsman Lists'");
            var defList = SanJS.WebAPI.getCRMRecordData("nmc_newsmanconfigs", "nmc_value", "nmc_name eq 'Default List'");
            if (nmLists) {
                self.nmLists(JSON.parse(nmLists.nmc_value));
                if (defList) {
                    self.nmSelectedList(defList.nmc_value);
                }
            }
            else {
                setTimeout(self.nmLoadLists(), 500);
            }
        }
    }

    self.nmLoadSegments = function () {
        return [];
    }

    self.SaveApiInfo = function () {
        if (!self.nmApiKeyLoaded()) {
            var apikeyConfig = {};
            apikeyConfig.nmc_name = "ApiKey";
            apikeyConfig.nmc_value = self.nmApiKey();

            //create param
            SanJS.WebAPI.createCRMRecord("nmc_newsmanconfigs", apikeyConfig, null, SanJS.WebAPI.errorHandler);
            self.nmApiKeyLoaded(true);
        }
        else {
            //update param
            var resp = SanJS.WebAPI.getCRMRecordData("nmc_newsmanconfigs", "nmc_value", "nmc_name eq 'ApiKey'");
            if (resp) {
                var paramId = resp.nmc_newsmanconfigid;
                var p = { "value": self.nmApiKey().toString() };
                SanJS.WebAPI.updateCRMRecordAttribute(p, paramId, "nmc_newsmanconfigs", "nmc_value");
            }
        }

        if (!self.nmUserIdLoaded()) {
            var useridConfig = {};
            useridConfig.nmc_name = "UserId";
            useridConfig.nmc_value = self.nmUserId();

            //create param
            SanJS.WebAPI.createCRMRecord("nmc_newsmanconfigs", useridConfig, null, SanJS.WebAPI.errorHandler);
            self.nmUserIdLoaded(true);
        }
        else {
            //update param
            var resp = SanJS.WebAPI.getCRMRecordData("nmc_newsmanconfigs", "nmc_value", "nmc_name eq 'UserId'");
            if (resp) {
                var paramId = resp.nmc_newsmanconfigid;
                var p = { "value": self.nmUserId().toString() };
                SanJS.WebAPI.updateCRMRecordAttribute(p, paramId, "nmc_newsmanconfigs", "nmc_value");
            }
        }

        refreshPage();
    }

    self.SaveConfig = function () {
        return;
    }

    self.SaveDefaultList = function () {
        //check if Default List parameter exists
        var resp = SanJS.WebAPI.getCRMRecordData("nmc_newsmanconfigs", "nmc_value", "nmc_name eq 'Default List'");
        if (resp) {
            if (self.nmSelectedList()) {
                var paramId = resp.nmc_newsmanconfigid;
                var p = { "value": self.nmSelectedList().toString() };
                SanJS.WebAPI.updateCRMRecordAttribute(p, paramId, "nmc_newsmanconfigs", "nmc_value");
            }
        }
            //create if it does not
        else {
            var p = { "nmc_name": "Default List", "nmc_value": self.nmSelectedList() };
            SanJS.WebAPI.createCRMRecord("nmc_newsmanconfigs", p, null, SanJS.WebAPI.errorHandler);
        }
    };

    self.SaveDefaultSegment = function () {
        //save to crm
    };

    self.init();
}

function retrieveCRMConfiguration() {
    var self = this;
    var vmInfo = {};

    var resp = SanJS.WebAPI.getCRMRecordData("nmc_newsmanconfigs", "nmc_value", "nmc_name eq 'ApiKey'");
    vmInfo.akConfig = resp ? resp.nmc_value : null;
    resp = SanJS.WebAPI.getCRMRecordData("nmc_newsmanconfigs", "nmc_value", "nmc_name eq 'UserId'");
    vmInfo.uidConfig = resp ? resp.nmc_value : null;

    return vmInfo;
}

function refreshPage() {
    // Get the link object to simulate user click
    var reload = document.getElementById('reload');

    // Assign the modal url to the link then click!
    reload.href = window.location.href;
    reload.click();
}