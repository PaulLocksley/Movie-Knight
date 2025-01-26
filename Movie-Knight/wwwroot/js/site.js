// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function filterTable(type, role, name){
    const params =  new Proxy(new URLSearchParams(window.location.search), {
        get: (searchParams, prop) => searchParams.get(prop),
    });
    let filterString = params.filterString;
    if(filterString === null){
        filterString = []
    }else{
        filterString = JSON.parse(filterString);
    }
    filterString.push({"type":type,"role":role,"name":name})
    let users = params.userNames;
    console.log(type, role,name ,users ,filterString )

    htmx.ajax('GET', '/UserComparison?userNames='+users+'&filterString='+JSON.stringify(filterString),
        {target:'#userAnalysis', swap:'innerHTML'}).then(() => {
        // this code will be executed after the 'htmx:afterOnLoad' event,
        // and before the 'htmx:xhr:loadend' event
        var url = window.location.protocol + "//" + window.location.host + window.location.pathname + '?userNames='+users+'&filterString='+JSON.stringify(filterString);
        window.history.pushState({path:url},'',url);

        console.log('Content inserted successfully!');
    });

}


function openTab(evt, viewName) {
    // Declare all variables
    var i, tabcontent, tablinks;
    if (!graphsLoaded && viewName === "graphView"){
        htmx.ajax('GET', '/_Graph?'+ new URLSearchParams(window.location.search),
            {target:'#graphView', swap:'innerHTML'})
        graphsLoaded = true
    }
    if (!watchListLoaded && viewName === "watchListView"){
        htmx.ajax('GET','/_WatchList?'+ new URLSearchParams(window.location.search),
            {target:'#watchListView',swap:'innerHTML'})
        watchListLoaded = true
    }
    // Get all elements with class="tabcontent" and hide them
    tabcontent = document.getElementsByClassName("tabcontent");
    for (i = 0; i < tabcontent.length; i++) {
        tabcontent[i].style.display = "none";
    }

    // Get all elements with class="tablinks" and remove the class "active"
    tablinks = document.getElementsByClassName("tablinks");
    for (i = 0; i < tablinks.length; i++) {
        tablinks[i].className = tablinks[i].className.replace(" active", "");
    }

    // Show the current tab, and add an "active" class to the button that opened the tab
    document.getElementById(viewName).style.display = "block";
    evt.currentTarget.className += " active";
    setTimeout(() => {
        document.getElementById(viewName).style.display = "block";
    }, 50);
}