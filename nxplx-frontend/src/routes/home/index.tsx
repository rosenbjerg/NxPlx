import { createSnackbar } from '@egoist/snackbar'
import linkState from 'linkstate';
import orderBy from 'lodash/orderBy';
import { Component, h } from 'preact';
// @ts-ignore
import Helmet from 'preact-helmet';
import {Link, route} from "preact-router";
import Loading from '../../components/loading';
import { imageUrl } from "../../Details";
import http from '../../Http';
import * as style from './style.css';
import { Info } from "../../models";


interface Props {}

interface State { overview?: Info[]; progress?: object; search:string }

export default class Home extends Component<Props, State> {

    public state = {
        overview: undefined,
        progress: undefined,
        search: ''
    };

    public componentDidMount() : void {
        this.load();
    }


    public render(props:Props, { overview, search }: State) {
        return (
            <div class={style.home}>
                <Helmet title="NxPlx" />
                <div class={style.top}>
                    <input tabIndex={0} autofocus class={style.search} placeholder="search here" type="search" onInput={linkState(this, 'search')} />
                    {/*<button tabIndex={0} class={['material-icons', style.scan].join(' ')} title="Scan library files" onClick={this.scan}>refresh</button>*/}
                </div>

                {overview === undefined ? (
                    <Loading />
                ) : (
                    <div class={style.entryContainer}>
                        {overview
                            .filter(this.entrySearch(search))
                            .map(entry => (
                                    <Link key={entry.id} href={`/${entry.kind}/${entry.id}`}>
                                        <img key={entry.id} class={style.entryTile} src={imageUrl(entry.poster, 154)} title={entry.title} alt={entry.title} />
                                    </Link>
                                )
                            )}
                    </div>
                )}
            </div>
        );
    }
    private entrySearch = (search:string) => (entry:Info) => {
        const lowercaseSearch = search.toLowerCase();
        return  entry.kind.includes(lowercaseSearch) ||
                entry.title.toLowerCase().includes(lowercaseSearch);
    };

    private load = () => {
        http.get('/api/overview')
            .then(async response => {
                if (response.ok) {
                    const overview = await response.json();
                    this.setState({ overview: orderBy(overview, ['title'], ['asc']) });
                }
                else {
                    route('/login');
                }
            })
    };

    // private scan = () => {
    //     const scanning = createSnackbar('Scanning library...', { timeout: 1500 });
    //     http.post('/api/scan', '', false).then(response => {
    //         if (!response.ok) {
    //             scanning.destroy();
    //             createSnackbar('Scanning failed :/', { timeout: 1500 });
    //         }
    //         else {
    //             scanning.destroy();
    //             createSnackbar('Scan completed', { timeout: 1500 });
    //             this.load();
    //         }
    //     })
    // }
}
