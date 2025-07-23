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

// Shared variables and functions for movie row expansion
var fetchedMovies = []

function showHidenRow(rowId) { 
    let r = document.getElementById(rowId)
    let img = document.getElementById("img-"+rowId)
    console.log(fetchedMovies)
    let id = rowId.split("-")[1]

    if(!fetchedMovies.includes(id)){
        htmx.ajax('GET', '/_filters?movieId='+id, 
                    {target:'#'+rowId, swap:'outerHTML'})
                    /*.then(() => {
                    document.getElementById(rowId).style.display = "contents"
                    })*/
        fetchedMovies.push(id)
        return
    }
    let current = r.style.display 
    console.log(current)
    if(current !== "none"){
        r.style.display = "none"  
        img.src = "img/expand_more.svg"  
    }else{
        r.style.display = "contents"   
        img.src = "img/expand_less.svg"  
    }
} 

// Tab management functions
var loadedTabs = new Set();

function showTab(tabName) {
    // Hide all tab panes
    document.querySelectorAll('.tab-pane').forEach(pane => {
        pane.style.display = 'none';
    });
    
    // Remove active class from all tabs
    document.querySelectorAll('.tablinks').forEach(tab => {
        tab.classList.remove('active');
    });
    
    // Show the selected tab pane
    document.getElementById(tabName + '-tab').style.display = 'block';
    
    // Add active class to the clicked tab
    event.target.classList.add('active');
}

// Clear loaded tabs when new user comparison data is loaded
function clearLoadedTabs() {
    loadedTabs.clear();
}

// Listen for HTMX events to clear loaded tabs when new user comparison data is loaded
document.addEventListener('htmx:afterSwap', function(event) {
    // Clear loaded tabs when the user analysis content is updated
    if (event.target.id === 'userAnalysis') {
        clearLoadedTabs();
    }
});

function loadHtmxTab(tabName, url, buttonElement) {
    // Hide all tab panes
    document.querySelectorAll('.tab-pane').forEach(pane => {
        pane.style.display = 'none';
    });
    
    // Remove active class from all tabs
    document.querySelectorAll('.tablinks').forEach(tab => {
        tab.classList.remove('active');
    });
    
    // Add active class to the clicked tab
    buttonElement.classList.add('active');
    
    // Show the selected tab pane
    const tabPane = document.getElementById(tabName + '-tab');
    tabPane.style.display = 'block';
    
    // Load content via HTMX if not already loaded
    if (!loadedTabs.has(tabName)) {
        htmx.ajax('GET', url, {
            target: '#' + tabName + '-tab',
            swap: 'innerHTML'
        });
        loadedTabs.add(tabName);
    }
}


