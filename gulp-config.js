module.exports = function () {
    var sitecoreRoot = "C:\\Sitecore\\Sandbox-82";
    var config = {
        websiteRoot: sitecoreRoot + "\\Website",
        sitecoreLibraries: sitecoreRoot + "\\Website\\bin",
        solutionName: "ImageOptimization",
        licensePath: sitecoreRoot + "\\Data\\license.xml",
        runCleanBuilds: false
    };
    return config;
}