// ApexCharts interop file

// Store chart instances to allow cleanup
let charts = {};

// Date formatter functions
const dateFormatter = (value) => {
    if (!value) return '';
    return new Date(value).toLocaleDateString();
};

// Initialize a dumbbell chart
function initDumbbellChart(elementId, options) {
    // Destroy any existing chart
    destroyChart(elementId);

    // Process the options to convert string functions to actual functions
    if (options && options.xaxis && options.xaxis.labels && options.xaxis.labels.formatter) {
        if (typeof options.xaxis.labels.formatter === 'string') {
            options.xaxis.labels.formatter = dateFormatter;
        }
    }

    if (options && options.tooltip && options.tooltip.x && options.tooltip.x.formatter) {
        if (typeof options.tooltip.x.formatter === 'string') {
            options.tooltip.x.formatter = dateFormatter;
        }
    }

    // Wait until the element exists in the DOM
    let checkInterval = setInterval(() => {
        const element = document.getElementById(elementId);
        if (element) {
            clearInterval(checkInterval);

            // Load ApexCharts if it's not already loaded
            if (typeof ApexCharts === 'undefined') {
                loadApexChartsLibrary().then(() => {
                    createChart(elementId, options);
                });
            } else {
                createChart(elementId, options);
            }
        }
    }, 100);
}

function createChart(elementId, options) {
    try {
        console.log("Creating chart with options:", options);
        const chart = new ApexCharts(document.getElementById(elementId), options);
        charts[elementId] = chart;
        chart.render();
    } catch (error) {
        console.error("Error creating chart:", error);
    }
}

function destroyChart(elementId) {
    if (charts[elementId]) {
        try {
            charts[elementId].destroy();
            delete charts[elementId];
        } catch (e) {
            console.error("Error destroying chart:", e);
        }
    }
}

function loadApexChartsLibrary() {
    return new Promise((resolve, reject) => {
        // Check if ApexCharts is already loaded
        if (typeof ApexCharts !== 'undefined') {
            resolve();
            return;
        }

        // Load CSS
        const linkElement = document.createElement('link');
        linkElement.rel = 'stylesheet';
        linkElement.href = 'https://cdn.jsdelivr.net/npm/apexcharts@3.37.1/dist/apexcharts.css';
        document.head.appendChild(linkElement);

        // Load JavaScript
        const scriptElement = document.createElement('script');
        scriptElement.src = 'https://cdn.jsdelivr.net/npm/apexcharts@3.37.1/dist/apexcharts.min.js';
        scriptElement.onload = () => resolve();
        scriptElement.onerror = (error) => reject(error);
        document.head.appendChild(scriptElement);
    });
}

// Make sure functions are exposed to .NET
window.initDumbbellChart = initDumbbellChart;
window.destroyChart = destroyChart;