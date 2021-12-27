// Example class. You can also see the vidstreaming class for a complete example.
import { EventEmitter } from 'events';

/* 
    Required, or else the module wont be recognized by vidstreamdownloader.
    This class should have these methods (or else something bad might happen!): 

    getEpisodes(searchTerm - basically argsObj.searchTerm) - Returns an array with the chapters. 
    Should also emit events chapterDone and chapterProgress when required.

    download() - This should return an array with failed urls, in case there were one.
    In a future this class will be inherited to save some time.

    Events:
        "urlSlugProgress" is used for giving the user information about the download in the format of "Getting url for ${slug} (${current}/${total})...", it should emit an object with the following parameters:
            slug - The slug/anime name/episode
            current - The current URL thats being fetched
            total - The number of total URLS/episodes that will be fetched

        "urlProgressDone" is used to let the user know that the current url is done fetching. Outputs "Done!" in green color to the console.
        
*/
const source = class extends EventEmitter {
    /* 
    anime-dl passes two arguments to the constructor
      argsObj - An object with command line arguments and their values
      defaultDownloadFormat - The format that can be used to store resulting files,
      in case there is none specified by the user. Check help to see how to replace the %% values.
    */
    constructor(argsObj, defaultDownloadFormat) {
        super();
    }

    getEpisodes(searchTerm) {
        const getChapter = () => {
            this.emit('urlSlugProgress', {
                slug: 'chapter 2',
                current: 2,
                total: 2
            })
            return 'www.animesite.com/videos/chapter2.mp4';
        }
        let chapterURL = getChapter(searchTerm)
        this.emit('urlProgressDone')
        // Once all the chapters are get, return their url
        return [chapterURL]
    }

    async download() {
        let episodesToDownload = ['episode 1', 'episode 2']
        let failedEpisodes = ['episode 3'];
        // In practice you would use the function "downloadWrapper" from utils/video.js (see the source for usage and see other downloaders for examples)
        await episodesToDownload.asyncForEach(async e => {
            process.stdout.write(`Downloading ${e}... `);
            return new Promise((res, rej) => {
                setTimeout(() => {
                    process.stdout.write('Done!\n');
                    res();
                }, 2000)
            })
            
        })
        return failedEpisodes;
    }
}

/* 
    module.exports.data is optional.
    it also might have as much parameters as you want. They will all be displayed on the 
    -lsc.
*/
const data = {
    name: 'mysitename',
    description: 'Cool anime site'
}

export default { source, data }; 