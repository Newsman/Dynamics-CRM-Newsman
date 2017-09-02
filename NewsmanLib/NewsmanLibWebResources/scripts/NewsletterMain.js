function onLoad() {
    Xrm.Page.getControl("IFRAME_newsletterinfo").setSrc(Xrm.Page.getAttribute("nmc_newsletterlink").getValue());
}