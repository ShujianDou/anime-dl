import { EventEmitter } from 'events';
import fetch from 'node-fetch';
import cheerio from 'cheerio';
import video from '../../utils/video.js';
import Source from '../../utils/source.js';
import { makeURLRegex } from '../../utils/url.js';

const windowNuxt = "window.__NUXT__=";
const getEpManifest = (query) => {
    // get script content and remove window.__NUXT__= and the final semicolon so it can be parsed by JSON.parse
    let manifest = JSON.parse(query("#__nuxt")[0].next.children[0].data.slice(windowNuxt.length, -1));
    return manifest.state.data.video;
}
const constEpUrl = `https://hanime.tv/videos/hentai`;
const epURLRegex = makeURLRegex(constEpUrl);
const loadCheerioEp = async (slug) => {
    let req = await fetch(constEpUrl + '/' + slug);
    let page = await req.text();
    return cheerio.load(page);
}

const getEpUrl = (manifest) => manifest.videos_manifest.servers[0].streams[1].url


const source = class Hanime extends Source {

    constructor() {
        super();
        this.slug = null;
    }

    async search(term) {
        if(epURLRegex.test(term)) {
            return {
                slug: term.split('/').slice(-1)[0]
            }
        }
        let req = await fetch("https://search.htv-services.com/", {
            "headers": {
                "content-type": "application/json"
            },
            "body": JSON.stringify({
                "search_text": term,
                "tags": [],
                "tags_mode": "AND",
                "brands": [],
                "blacklist": [],
                "order_by": "created_at_unix",
                "ordering": "asc",
                "page": 0
            }),
            "method": "POST"
        })

        let json = await req.json();

        let hits = JSON.parse(json.hits);

        if(hits.length < 1) {
            return {
                error: 'Could not find the desired term in HAnime, try with a more specific search.'
            }
        }

        return hits[0];
    }

    async getEpisodes(hits) {
        if(hits?.error) {
            return hits;
        }
        let epSlug = hits.slug;
        this.slug = epSlug.split('-').slice(0, -1).join('-');

        global.logger.debug(this.slug)
        
        this.emit('urlSlugProgress', {
            slug: this.slug,
            current: 1,
            total: '-'
        })
        let $ = await loadCheerioEp(epSlug)
        let manifest = getEpManifest($);
        let eps;
        let urls = [];
        try {
            eps = manifest.hentai_franchise_hentai_videos.filter(vid => vid.slug.startsWith(this.slug) && vid.slug !== epSlug)
        } catch {
            eps = [];
        }
        global.logger.debug(`${eps.length}+1 episodes`)
        this.emit('urlProgressDone');
        
        
        urls.push(getEpUrl(manifest));

        await eps.asyncForEach(async (ep, i) => {
            this.emit('urlSlugProgress', {
                slug: this.slug,
                current: i+2,
                total: eps.length+1
            })
            $ = await loadCheerioEp(ep.slug);
            urls.push(getEpUrl(getEpManifest($)));
            this.emit('urlProgressDone');
        })

        this.urls = urls;

        return urls;
    }

    async download() {
        return video.downloadWrapper({
            slug: this.slug,
            urls: this.urls
        })
    }
}

/* 
    module.exports.data is optional.
    it also might have as much parameters as you want. They will all be displayed on the 
    -lsc.
*/
const data = {
    name: 'Hanime',
    website: 'hanime.tv',
    description: 'Watch hentai online free download HD on mobile phone tablet laptop desktop. Stream online, regularly released uncensored, subbed, in 720p and 1080p!',
    language: 'English',
    _SEARCHREGEX: epURLRegex
}

export default { source, data }
