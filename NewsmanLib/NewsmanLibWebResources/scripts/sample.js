var viewModel;
var idProducatorCaussade = "42CAD4EB-3463-E411-80C7-00155D116F2F";
var speciiVara; //["Porumb", "Floarea soarelui", "Sorg", "Soia"];
var speciiToamna; //["Rapita", "Triticale", "Orz", "Grau"];
var codificari;
var codificariSpecie;
var monthNames;
//Implementare dirty flag
ko.dirtyFlag = function (root, isInitiallyDirty) {
    var result = function () { },
        _initialState = ko.observable(ko.toJSON(root)),
        _isInitiallyDirty = ko.observable(isInitiallyDirty);

    result.isDirty = ko.computed(function () {
        return _isInitiallyDirty() || _initialState() !== ko.toJSON(root);
    });

    result.reset = function () {
        _initialState(ko.toJSON(root));
        _isInitiallyDirty(false);
    };

    return result;
};
//End implementare dirty flag

$(document).ready(function () {
    var recId = getRecordIdFromQueryString();
    if (recId != null && recId != "") {
        codificari = getLabels("F");
        //getLabelForCode(codificari, "F1");
        codificariSpecie = getSpecii();
        speciiVara = getSpeciiBySet(1);
        speciiToamna = getSpeciiBySet(2);
        monthNames = [getLabelForCode('F12'), getLabelForCode('F13'), getLabelForCode('F14'), getLabelForCode('F15'), getLabelForCode('F16'), getLabelForCode('F17'),
        getLabelForCode('F23'), getLabelForCode('F24'), getLabelForCode('F25'), getLabelForCode('F26'), getLabelForCode('F10'), getLabelForCode('F11')];

        viewModel = new CsdViewModel(recId);
        ko.applyBindings(viewModel);
        $.each(viewModel.grupuri(), function (index, value) {
            SetDistribuitor(value);
        });
    }
});

//MultiLanguage
function getLabelForCode(code) {
    for (var i = 0; i < codificari.length; i++) {
        if (codificari[i].cod == code)
            return codificari[i].text;
    }

    return "N/A";
};

function getSpeciiBySet(set) {
    var list = new Array();
    for (var i = 0; i < codificariSpecie.length; i++) {
        if (codificariSpecie[i].categorie.Value == set)
            list.push(codificariSpecie[i].nume);
    }
    return list;
}

var CsdViewModel = function (recordId) {
    var self = this;

    //MultiLanguage
    self.getLabelForCode = function (code) {
        for (var i = 0; i < codificari.length; i++) {
            if (codificari[i].cod == code)
                return codificari[i].text;
        }

        return "N/A";
    };
    self.show = ko.observable(true);

    //STATIC
    self.campanieid = "";
    self.tipForecast = ko.observable(0);
    self.isPromoter = ko.observable(false);
    self.grupuri = ko.observableArray([]);
    self.forecastid = recordId;
    self.updated = false;
    self.fcantitate = ko.computed(function () {
        var c = 0;
        $.each(self.grupuri(), function (i, v) {
            var cc = parseFloat(v.cantitate());
            if (!isNaN(cc))
                c += cc;
        });
        return formatNumber(c, 2, false);
    });
    self.ftotal = ko.computed(function () {
        var t = 0;
        $.each(self.grupuri(), function (i, v) {
            var tt = parseFloat(v.total());
            if (!isNaN(tt))
                t += tt;
        });
        return formatNumber(t, 2, false);
    });
    self.totalRealizat = ko.computed(function () {
        var tR = 0;
        $.each(self.grupuri(), function (i, v) {
            $.each(v.specii(), function (j, w) {
                tR += w.realizatSpecie();
            });
        });
        return formatNumber(tR, 2, false);
    });

    self.init = function () {
        //preia date din CRM
        var recordInfo = GetCRMData(self.forecastid);
        self.campanieid = recordInfo.campanieid;
        self.tipForecast(recordInfo.tipForecast.Value);
        self.isPromoter(self.tipForecast() == 100000002);
        if (recordInfo.grupuri.length > 0)
            self.grupuri(recordInfo.grupuri);
        else {
            self.newGroup();
        }
    };

    self.newGroup = function () {
        self.grupuri.push(new GrupDistribuitor(null, null, null, self.forecastid, self.campanieid))
    }
    self.removeRow = function (item) {
        if (item.grupid != null) {
            deleteCRMRecord(item.grupid, "new_componenteforecast", function (response) {
                self.grupuri.remove(item);
            },
            function (response) {
                //alert("Eroare stergere linie din grup: " + response.responseText);
                alert(JSON.parse(req.responseText).error.message.value);
            });
        }
        else
            self.grupuri.remove(item);
    }

    self.save = function () {
        if (!self.updated) {
            self.updated = true;

            //salveaza parinte
            if (self.forecastid != null) {
                var updData = {};
                updData.new_Totalcantitate = !isNaN(self.ftotal()) ? self.ftotal() : null;
                updData.new_Totalforecast = !isNaN(self.fcantitate()) ? self.fcantitate() : null;
                updateCRMRecord(updData, self.forecastid, "new_forecast");
            }

            //salveaza informatii grupuri
            $.each(self.grupuri(), function (idx, val) {
                if (val.isDirty()) {
                    val.save();
                }
            });
        }
    };

    self.init();
    self.dirtyFlag = new ko.dirtyFlag(self);
    self.dirtyItems = ko.computed(function () {
        return ko.utils.arrayFilter(self.grupuri(), function (dt) {
            return dt.dirtyFlag.isDirty();
        });
    }, self);

    self.isDirty = ko.computed(function () {
        self.save();
        self.updated = false;
    }, self);
}

function GrupDistribuitor(distribuitorid, specii, grupid, forecastid, campanie) {
    var self = this;

    //MultiLanguage
    self.getLabelForCode = function (code) {
        for (var i = 0; i < codificari.length; i++) {
            if (codificari[i].cod == code)
                return codificari[i].text;
        }

        return "N/A";
    };

    self.distribuitori = ko.observableArray(Distribuitori());
    self.selectat = ko.observable(distribuitorid);
    self.forecastid = forecastid;
    self.grupid = grupid;
    self.campanie = campanie;
    self.created = false;
    self.specii = ko.observableArray(specii);
    self.init = function () {
        if (self.specii() == null || self.specii().length == 0) {
            //lista specii
            var listaSpecii = getCRMRecordsData("new_categorie",
                        "new_name,new_categorieId",
                        "new_CategorieParinteId/Id eq null");

            var tipCampanie = GetTipCampanie(self.campanie);

            for (var si = 0; si < listaSpecii.length; si++) {
                var specieTemplate = listaSpecii[si].new_name;
                var specieTemplateId = listaSpecii[si].new_categorieId;

                //nu include cereale de toamna pentru campanii de primavara
                if ($.inArray(specieTemplate, speciiVara) < 0 && tipCampanie == 100000000)
                    continue;
                //nu include cereale de primavara pentru campanii de toamna
                if ($.inArray(specieTemplate, speciiToamna) < 0 && tipCampanie == 100000001)
                    continue;

                var activ = false;
                var newGrupSpecie = new GrupSpecie(false, null, specieTemplate, specieTemplateId, self.grupid, self.forecastid, null, self.campanie);
                self.specii.push(newGrupSpecie);
            }
        }

        SetDistribuitor(self);
    }
    self.totalRealizat = ko.computed(function () {
        var tR = 0;
        $.each(self.specii(), function (i, v) {
            tR += v.realizatSpecie();
        });
        return formatNumber(tR, 2, false);
    });
    self.cantitate = ko.computed(function () {
        var c = 0;
        $.each(self.specii(), function (i, v) {
            if (v.items() != null) {
                $.each(v.items(), function (ii, vv) {
                    var cc = parseFloat(vv.cantitate());
                    if (!isNaN(cc))
                        c += cc;
                });
            }
        });
        return formatNumber(c, 2, false);
    });
    self.total = ko.computed(function () {
        var t = 0;
        $.each(self.specii(), function (i, v) {
            if (v.items() != null) {
                $.each(v.items(), function (ii, vv) {
                    var pp = parseFloat(vv.pretmediu());
                    var cc = parseFloat(vv.cantitate());
                    if (!isNaN(pp) && !isNaN(cc))
                        t += pp * cc;
                });
            }
        });
        return formatNumber(t, 2, false);
    });
    self.dirtyFlag = new ko.dirtyFlag(self);
    self.dirtyItems = ko.computed(function () {
        return ko.utils.arrayFilter(self.specii(), function (dt) {
            return dt.dirtyFlag.isDirty();
        });
    }, self);

    self.save = function () {
        //salveaza informatii parinte
        if (self.grupid != null) {
            var updData = {};
            updData.new_DistribuitorId = { Id: self.selectat(), LogicalName: "new_distribuitor", Name: "" };
            updData.new_Cantitate = isNaN(parseFloat(self.cantitate().replace(",", ""))) ? null : self.cantitate();
            updData.new_Pret = isNaN(parseFloat(self.total().replace(",", ""))) ? null : self.total();
            updateCRMRecord(updData, self.grupid, "new_componenteforecast");
        }
        else {
            if (self.selectat() != null && self.created == false) {
                var creData = {};
                self.created = true;
                creData.new_DistribuitorId = { Id: self.selectat(), LogicalName: "new_distribuitor", Name: "" };
                creData.new_ForecastId = { Id: self.forecastid, LogicalName: "new_forecast", Name: "" };
                createCRMRecord(creData, "new_componenteforecast", function (response) {
                    if (response != null && response.d != null) {
                        self.grupid = response.d.new_componenteforecastId;
                        self.init();
                    }
                });
            }
        }

        //salveaza copii
        $.each(self.specii(), function (i, v) {
            //if (v.isDirty()) {
            v.save();
            //}
        });
    };

    self.isDirty = ko.computed(function () {
        return (self.dirtyFlag.isDirty() || self.dirtyItems().length > 0);
    }, self);
}

function GrupSpecie(activ, distribuitorid, nume, specieid, grupid, forecastid, items, campanie) {
    var self = this;

    //MultiLanguage
    self.getLabelForCode = function (code) {
        for (var i = 0; i < codificari.length; i++) {
            if (codificari[i].cod == code)
                return codificari[i].text;
        }

        return "N/A";
    };

    self.activ = ko.observable(activ);
    self.created = false;
    self.distribuitorid = distribuitorid;
    self.forecastid = forecastid;
    self.items = ko.observableArray(items);
    self.specieid = specieid;
    self.nume = nume;
    self.campanie = campanie;
    self.idcomponenta = grupid;
    self.dirtyFlag = new ko.dirtyFlag(self);
    self.realizatSpecie = ko.computed(function () {
        var result = 0;
        $.each(self.items(), function (idx, val) {
            var rr = parseFloat(val.realizat());
            if (!isNaN(rr)) {
                result += rr;
            }
        });
        return result;
    });
    self.bugetSpecie = ko.computed(function () {
        var result = 0;
        $.each(self.items(), function (idx, val) {
            var bb = parseFloat(val.cantitate());
            if (!isNaN(bb)) {
                result += bb;
            }
        });
        return result;
    });
    self.realizatRatioSpecie = ko.computed(function () {
        return self.realizatSpecie() && self.bugetSpecie() ? formatNumber(self.realizatSpecie() * 100 / self.bugetSpecie(), 2, false) : null;
    });

    self.newRow = function () {
        self.items.push(new ItemAtts(self.specieid, nume, null, null, null, null, null, null, null, null, null, null, null, null, null, self.idcomponenta, self.campanie, null, viewModel.isPromoter()))
    }
    self.removeRow = function (item) {
        if (item.itemId != null && !item.deleted) {
            item.deleted = true;
            deleteCRMRecord(item.itemId, "new_instantaprodusforecast", function (response) {
                self.items.remove(item);
            },
            function (response) {
                //alert("Eroare stergere linie din grup: " + response.responseText);
                alert(JSON.parse(req.responseText).error.message.value);
            });
        }
        else
            self.items.remove(item);
    }

    self.save = function () {
        //verifica daca a fost dezactivat
        if (!self.activ()) {
            //sterge elementele asociate
            $.each(self.items(), function (idx, val) {
                if (val.itemId != null && !val.deleted) {
                    val.deleted = true;
                    deleteCRMRecord(val.itemId, "new_instantaprodusforecast", function (response) {
                        self.items.remove(val);
                    },
                    function (response) {
                        //alert("Eroare stergere linii din grup: " + response.responseText);
                        alert(JSON.parse(req.responseText).error.message.value);
                    });
                }
                else
                    self.items.remove(val);
            });
        }
        else {
            //salveaza informatii linii produse
            $.each(self.items(), function (idx, val) {
                //if (val.isDirty()) {
                val.saveRow();
                //}
            });
        }
    };

    self.dirtyItems = ko.computed(function () {
        return ko.utils.arrayFilter(self.items(), function (dt) {
            return dt.dirtyFlag.isDirty();
        });
    }, self);

    self.isDirty = ko.computed(function () {
        return (self.dirtyFlag.isDirty() || self.dirtyItems().length > 0);
    }, self);
}

var ItemAtts = function (specie, numeSpecie, prod, cantitate, pret, discount, l1, l2, l3, l4, l5, l6, l7, l8, itemId, parentid, campanie, realizat, isPromoter) {
    var self = this;

    //MultiLanguage
    self.getLabelForCode = function (code) {
        for (var i = 0; i < codificari.length; i++) {
            if (codificari[i].cod == code)
                return codificari[i].text;
        }

        return "N/A";
    };

    self.produsLista = ko.observableArray(ProduseProducatorPerSpecie(idProducatorCaussade, specie));
    self.specie = specie;
    self.numeSpecie = numeSpecie;
    self.selectat = ko.observable(prod);
    self.campanie = campanie;
    self.pret = ko.observable(formatNumber(pret, 2, false));
    self.discount = ko.observable(discount != null ? formatNumber(discount) : 0);
    self.created = false;
    self.deleted = false;
    var denumireLuni = GetDenumireLuni(campanie);
    self.l1name = denumireLuni[0];
    self.l2name = denumireLuni[1];
    self.l3name = denumireLuni[2];
    self.l4name = denumireLuni[3];
    self.l5name = denumireLuni[4];
    self.l6name = denumireLuni[5];
    self.l7name = denumireLuni[6];
    self.l8name = denumireLuni[7];
    self.setcomplet = ko.observable(denumireLuni.length == 8);
    self.l1 = ko.observable(formatNumber(l1, 2, false));
    self.l2 = ko.observable(formatNumber(l2, 2, false));
    self.l3 = ko.observable(formatNumber(l3, 2, false));
    self.l4 = ko.observable(formatNumber(l4, 2, false));
    self.l5 = ko.observable(formatNumber(l5, 2, false));
    self.l6 = ko.observable(formatNumber(l6, 2, false));
    self.l7 = ko.observable(formatNumber(l7, 2, false));
    self.l8 = ko.observable(formatNumber(l8, 2, false));
    self.cantitate = ko.observable(formatNumber(cantitate, 2, false));
    if (isPromoter) {
        self.realizat = ko.computed(function () {
            return formatNumber(GetRealizatProdusPromoter(self.selectat()), 2, false)
        }); //TODO: Trebuie calculat
    }
    else {
        self.realizat = ko.observable(formatNumber(realizat, 2, false));
    }
    self.realizatRatio = ko.computed(function () {
        return self.realizat() && self.cantitate() ? formatNumber(self.realizat() * 100 / self.cantitate(), 2, false) : null;
    });
    self.distributieCantitate = function () {
        var totalCantitate = self.cantitate() != null ? self.cantitate() : 0;
        var tipCampanie = GetTipCampanie(self.campanie);

        if (tipCampanie == 100000000) //Vara
        {
            self.l2(formatNumber(totalCantitate * 0.1, 2, false)); //10%
            self.l3(formatNumber(totalCantitate * 0.35, 2, false)); //35%
            self.l4(formatNumber(totalCantitate * 0.4, 2, false)); //40%
            self.l5(formatNumber(totalCantitate * 0.15, 2, false)); //15%
        }
        else if (tipCampanie == 100000001) //Toamna
        {
            if (self.numeSpecie == "Rapita") {
                self.l1(formatNumber(totalCantitate * 0.5, 2, false)); //50%
                self.l2(formatNumber(totalCantitate * 0.5, 2, false)); //50%
            }
            else {
                self.l1(formatNumber(totalCantitate * 0.2, 2, false)); //20%
                self.l2(formatNumber(totalCantitate * 0.6, 2, false)); //60%
                self.l3(formatNumber(totalCantitate * 0.2, 2, false)); //20%
            }
        }

    };
    self.pretmediu = ko.computed(function () {
        var p = self.pret() == null || isNaN(self.pret()) ? 0 : parseFloat(self.pret());
        var d = self.discount() == null || isNaN(self.discount()) ? 0 : parseFloat(self.discount());
        //var c = isNaN(self.cantitate()) ? 0 : parseFloat(self.cantitate());
        return formatNumber((p - p * d / 100)/* * c*/, 2, false);
    });
    self.parentId = parentid;
    self.itemId = itemId;
    self.init = function () {
        var pretDinCampanie = (pret == null) ? PretProdusCampanie(self.campanie, self.selectat()) : pret;
        self.pret(pretDinCampanie != null ? formatNumber(pretDinCampanie, 2, false) : 0);
        self.distributieCantitate();
    };
    self.init();
    self.dirtyFlag = new ko.dirtyFlag(self);

    self.saveRow = function () {
        if (self.itemId == null) {
            if (self.selectat() != null &&
                    self.specie != null &&
                    self.cantitate() != null &&
                    !isNaN(self.cantitate()) &&
                    !isNaN(self.realizat()) &&
                    !self.created) {
                var newItem = {};
                self.created = true;
                newItem.new_Discountpercent = formatNumber(self.discount(), 2, false);
                newItem.new_Pret = (self.pret() != null && self.pret() != "") ? formatNumber(self.pret(), 2, false) : null;
                newItem.new_ProdusId = { Id: self.selectat(), LogicalName: "new_produs", Name: "" }
                newItem.new_subiectId = { Id: self.specie, LogicalName: "new_categorie", Name: "" };
                newItem.new_ComponentaforecastId = { Id: self.parentId, LogicalName: "new_componenteforecast", Name: "" };
                newItem.new_Pretmediu = self.pretmediu();
                newItem.new_Luna1 = (self.l1() != null && self.l1() != "") ? formatNumber(self.l1(), 2, false) : null;
                newItem.new_Luna2 = (self.l2() != null && self.l2() != "") ? formatNumber(self.l2(), 2, false) : null;
                newItem.new_Luna3 = (self.l3() != null && self.l3() != "") ? formatNumber(self.l3(), 2, false) : null;
                newItem.new_Luna4 = (self.l4() != null && self.l4() != "") ? formatNumber(self.l4(), 2, false) : null;
                newItem.new_Luna5 = (self.l5() != null && self.l5() != "") ? formatNumber(self.l5(), 2, false) : null;
                newItem.new_Luna6 = (self.l6() != null && self.l6() != "") ? formatNumber(self.l6(), 2, false) : null;
                newItem.new_Luna7 = (self.l7() != null && self.l7() != "") ? formatNumber(self.l7(), 2, false) : null;
                newItem.new_Luna8 = (self.l8() != null && self.l8() != "") ? formatNumber(self.l8(), 2, false) : null;
                newItem.new_Cantitate = (self.cantitate() != null && self.cantitate() != "") ? formatNumber(self.cantitate(), 2, false) : null;
                newItem.new_realizat = (self.realizat() != null && self.realizat() != "") ? formatNumber(self.realizat(), 2, false) : null;
                newItem.new_realizat_ratio = (self.realizatRatio() != null && self.realizatRatio() != "") ? formatNumber(self.realizatRatio(), 2, false) : null;

                createCRMRecord(newItem, "new_instantaprodusforecast", function (response) {
                    if (response != null && response.d != null) {
                        self.itemId = response.d.new_instantaprodusforecastId;
                    }
                });
            }
        }
        else {
            if (self.selectat() != null &&
                    self.specie != null &&
                    self.pret() != null &&
                    self.cantitate() != null &&
                    !isNaN(self.cantitate())) {
                var crtItem = {};
                crtItem.new_Discountpercent = formatNumber(self.discount(), 2, false);
                crtItem.new_Pret = self.pret();
                crtItem.new_ProdusId = { Id: self.selectat(), LogicalName: "new_produs", Name: "" }
                crtItem.new_subiectId = { Id: self.specie, LogicalName: "new_categorie", Name: "" };
                crtItem.new_Pretmediu = self.pretmediu();
                crtItem.new_Luna1 = (self.l1() != null && self.l1() != "") ? formatNumber(self.l1(), 2, false) : null;
                crtItem.new_Luna2 = (self.l2() != null && self.l2() != "") ? formatNumber(self.l2(), 2, false) : null;
                crtItem.new_Luna3 = (self.l3() != null && self.l3() != "") ? formatNumber(self.l3(), 2, false) : null;
                crtItem.new_Luna4 = (self.l4() != null && self.l4() != "") ? formatNumber(self.l4(), 2, false) : null;
                crtItem.new_Luna5 = (self.l5() != null && self.l5() != "") ? formatNumber(self.l5(), 2, false) : null;
                crtItem.new_Luna6 = (self.l6() != null && self.l6() != "") ? formatNumber(self.l6(), 2, false) : null;
                crtItem.new_Luna7 = (self.l7() != null && self.l7() != "") ? formatNumber(self.l7(), 2, false) : null;
                crtItem.new_Luna8 = (self.l8() != null && self.l8() != "") ? formatNumber(self.l8(), 2, false) : null;
                crtItem.new_Cantitate = (self.cantitate() != null && self.cantitate() != "") ? formatNumber(self.cantitate(), 2, false) : null;
                crtItem.new_realizat = (self.realizat() != null && self.realizat() != "") ? formatNumber(self.realizat(), 2, false) : null;
                crtItem.new_realizat_ratio = (self.realizatRatio() != null && self.realizatRatio() != "") ? formatNumber(self.realizatRatio(), 2, false) : null;

                updateCRMRecord(crtItem, self.itemId, "new_instantaprodusforecast");
            }
        }
    }
    self.setPret = function () {
        if (isNaN(self.pret()) || self.pret() == "") {
            self.pret(formatNumber(PretProdusCampanie(self.campanie, self.selectat()), 2, false));
        };
    };

    self.isDirty = ko.computed(function () {
        return self.dirtyFlag.isDirty();
    }, self);
}

function GetCRMData(recordId) {
    var self = this;

    var mainInfo = getCRMRecordData("new_forecast", "new_CampanieId,new_forecastId,new_TipForecast", "new_forecastId eq guid'" + recordId + "'")
    var vmInfo = new Object();
    var tipCampanie = 0;

    if (mainInfo.new_CampanieId != null) {
        vmInfo.campanieid = mainInfo.new_CampanieId.Id;
        tipCampanie = GetTipCampanie(mainInfo.new_CampanieId.Id);
        //Vara   = 100000000
        //Toamna = 100000001
    }
    vmInfo.tipForecast = mainInfo.new_TipForecast
    // Promoter = 100000002
    // Director Regional = 100000001
    vmInfo.grupuri = new Array();
    vmInfo.recordId = recordId;

    var isPromoter = vmInfo.tipForecast.Value == 100000002;

    //lista specii
    var listaSpecii = getCRMRecordsData("new_categorie",
                "new_name,new_categorieId",
                "new_CategorieParinteId/Id eq null");

    //GRUPURI DISTRIBUITORI
    var listaDistribuitori = getCRMRecordsData("new_componenteforecast",
                "new_DistribuitorId,new_componenteforecastId",
                "new_ForecastId/Id eq guid'" + recordId + "'");
    for (var di = 0; di < listaDistribuitori.length; di++) {
        var newSpecii = new Array();
        for (var si = 0; si < listaSpecii.length; si++) {
            var specieTemplate = listaSpecii[si].new_name;
            var specieTemplateId = listaSpecii[si].new_categorieId;

            //nu include cereale de toamna pentru campanii de primavara
            if ($.inArray(specieTemplate, speciiVara) < 0 && tipCampanie == 100000000)
                continue;
            //nu include cereale de primavara pentru campanii de toamna
            if ($.inArray(specieTemplate, speciiToamna) < 0 && tipCampanie == 100000001)
                continue;

            var grupSpecie = getCRMRecordsData("new_instantaprodusforecast",
                    "new_subiectId,new_Discountpercent,new_Pret,new_Cantitate,new_instantaprodusforecastId,new_Luna1,new_Luna2,new_Luna3,new_Luna4,new_Luna5,new_Luna6,new_Luna7,new_Luna8,new_ProdusId,new_realizat",
                    "new_ComponentaforecastId/Id eq guid'" + listaDistribuitori[di].new_componenteforecastId + "' and new_subiectId/Id eq guid'" + specieTemplateId + "'");

            var activ = (grupSpecie.length > 0);
            var newGrupSpecie;
            if (activ) {
                var lineitems = new Array();
                $.each(grupSpecie, function (idx, val) {
                    lineitems.push(
                            new ItemAtts(specieTemplateId, specieTemplate, val.new_ProdusId.Id, val.new_Cantitate, val.new_Pret, val.new_Discountpercent,
                                        val.new_Luna1, val.new_Luna2, val.new_Luna3, val.new_Luna4, val.new_Luna5,
                                        val.new_Luna6, val.new_Luna7, val.new_Luna8, val.new_instantaprodusforecastId,
                                        listaDistribuitori[di].new_componenteforecastId, vmInfo.campanieid, val.new_realizat, isPromoter)
                        );
                });
                newGrupSpecie = new GrupSpecie(activ, listaDistribuitori[di].new_DistribuitorId.Id, specieTemplate, specieTemplateId, listaDistribuitori[di].new_componenteforecastId, recordId, lineitems, vmInfo.campanieid);
            }
            else {
                newGrupSpecie = new GrupSpecie(false, listaDistribuitori[di].new_DistribuitorId.Id, specieTemplate, specieTemplateId, listaDistribuitori[di].new_componenteforecastId, recordId, null, vmInfo.campanieid);
            }
            newSpecii.push(newGrupSpecie);
        }
        vmInfo.grupuri.push(new GrupDistribuitor(listaDistribuitori[di].new_DistribuitorId.Id, newSpecii,
            listaDistribuitori[di].new_componenteforecastId, recordId, vmInfo.campanieid));
    }

    return vmInfo;
}

function GetTipCampanie(idcampanie) {
    var dateCmp = getCRMRecordData("new_campanie", "new_Numecampanie", "new_campanieId eq guid'" + idcampanie + "'");
    return dateCmp.new_Numecampanie.Value;
}

function Distribuitori() {
    var dstrbs = getAllCRMRecordsData("new_distribuitor", "new_distribuitorId,new_name", "statecode/Value eq 0");
    var dstrbsList = new Array();
    $.each(dstrbs, function (index, val) {
        dstrbsList.push({
            value: val.new_distribuitorId,
            text: val.new_name
        });
    });

    return dstrbsList.sort(function (a, b) {
        if (a.text < b.text) return -1;
        if (b.text < a.text) return 1;
        return 0;
    });
}

function GetDenumireLuni(idCampanie) {
    var lista = {};
    var dateCmp = getCRMRecordData("new_campanie", "new_Datainceput, new_Datasfarsit", "new_campanieId eq guid'" + idCampanie + "'");
    if (dateCmp.new_Datainceput != null && dateCmp.new_Datasfarsit != null) {
        var d1 = new Date(parseInt(dateCmp.new_Datainceput.replace("/Date(", "").replace(")/", "")));
        var d2 = new Date(parseInt(dateCmp.new_Datasfarsit.replace("/Date(", "").replace(")/", "")));

        lista = MonthDiff(d1, d2);
    }

    return lista;
}

function MonthDiff(datFrom, datTo) {
    var arr = [];
    var fromYear = datFrom.getFullYear();
    var toYear = datTo.getFullYear();
    var diffYear = (12 * (toYear - fromYear)) + datTo.getMonth();

    for (var i = datFrom.getMonth() ; i <= diffYear; i++) {
        arr.push(monthNames[i % 12]/* + " " + Math.floor(fromYear + (i / 12))*/);
    }

    return arr;
}

function PretProdusCampanie(campanie, produs) {
    if (campanie != null && produs != null) {
        var preturi = getCRMRecordData("new_preturiprodusepecampanie", "new_Pret", "new_CampanieId/Id eq guid'" + campanie + "'" + " and new_ProdusId/Id eq guid'" + produs + "'");
        if (preturi != null) return preturi.new_Pret;
    }

    return 0;
}

function SetDistribuitor(grupDistribuitor) {
    if (viewModel.isPromoter()) {
        var faraDistribuitorName = "Fara Distribuitor";
        $.each(grupDistribuitor.distribuitori(), function (index, value) {
            if (value.text == "Fara Distribuitor") {
                grupDistribuitor.selectat(value.value);
            }
        });
    }
}

function GetRealizatProdusPromoter(produsId) {
    var forecastId = getRecordIdFromQueryString();
    var forecastObj = getCRMRecordData("new_forecast", "new_CampanieId, new_directorid", "new_forecastId eq guid'" + forecastId + "'");
    var campanieId = forecastObj.new_CampanieId.Id;
    var promoterId = forecastObj.new_directorid.Id;

    var itemList = getAllCRMRecordsData("new_comandasocietate",
        "new_new_comandasocietate_new_instantaproduscomandasocietate_ComandasocietateId/new_ProdusId, new_new_comandasocietate_new_instantaproduscomandasocietate_ComandasocietateId/new_NrDozecomandate",
                                        "new_CampanieId/Id eq guid'" + campanieId + "' and new_userid/Id eq guid'" + promoterId + "'", "",
                                        "new_new_comandasocietate_new_instantaproduscomandasocietate_ComandasocietateId");
    var retval = 0;

    $.each(itemList, function (idx, val) {
        $.each(val.new_new_comandasocietate_new_instantaproduscomandasocietate_ComandasocietateId.results, function (index, value) {
            if (value.new_ProdusId.Id == produsId) {
                retval += parseFloat(value.new_NrDozecomandate);
            }
        });
    });

    return retval;
}

(function () {
    window.formatNumber = function (value, precision, localize) {
        /// <summary>Rounds, removes trailing zeros and optionally localizes floating point numbers</summary>
        /// <param name="value" type="Number">Input value to prettify</param>
        /// <param name="precision" type="Number">Decimal to round off at</param>
        /// <param name="localize" type="Boolean">Localize the output to the current culture's format</param>
        /// <returns type="String">A prettified floating point number, as a string</returns>
        if (value == null) return null;
        //if (!value) value = "";
        if (!precision) precision = 0;
        if (!localize) localize = false;
        var rounded =
        (!isNaN(precision) && parseInt(precision) > 0)
        ? parseFloat(value).toFixed(parseInt(precision))
        : value;
        var trimmed = parseFloat(rounded).toString();
        if (localize && !isNaN(trimmed)) {
            return parseFloat(trimmed).toLocaleString();
        }
        else {
            return trimmed;
        }
    };
})(window);