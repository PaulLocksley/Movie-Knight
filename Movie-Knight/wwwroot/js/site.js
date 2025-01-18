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
