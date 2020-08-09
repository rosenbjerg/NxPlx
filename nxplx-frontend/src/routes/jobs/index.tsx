import { Component, h, VNode } from "preact";
import * as style from "./style.css";
import http from "../../utils/http";
import Loading from "../../components/Loading";
import Collapsible from "../../components/Collapsible";
import { translate } from "../../utils/localisation";


interface JobQueue<TJob> {
    name: string
    jobs: TJob[]
}

interface Job {
    method: string
}

interface SucceededJob extends Job {
    executionTime: number
}

interface FailedJob extends Job {
    exceptionType: string
    exceptionMessage: string
    exceptionDetails: string
}

interface JobViewProps {
    type: "enqueued" | "processing" | "succeeded" | "failed"
    children: VNode
}

interface JobViewState {
    count: number
}

const getIntervalTime = () => 9000 + (5000*Math.random());

class JobView extends Component<JobViewProps, JobViewState> {
    timeout: any = null;
    componentDidMount() {
        this.update();
    }
    componentWillUnmount() {
        if (this.timeout) clearTimeout(this.timeout);
    }
    update = () => http.getJson<number>(`/api/jobs/${this.props.type}/count`).then(count => {
        this.timeout = setTimeout(this.update, getIntervalTime());
        this.setState({ count })
    });

    render({ children }: JobViewProps, { count }: JobViewState) {
        return (
            <Collapsible title={`${translate(`${this.props.type} jobs`)}: ${count === undefined ? "?" : count}`}>
                {children}
            </Collapsible>
        );
    }
}

interface EnqueuedJobsListState {
    queues: JobQueue<Job>[]
}

class EnqueuedJobsList extends Component<{}, EnqueuedJobsListState> {
    timeout: any = null;
    componentDidMount() {
        this.update();
    }
    componentWillUnmount() {
        if (this.timeout) clearTimeout(this.timeout);
    }
    update = () => http.getJson<JobQueue<Job>[]>(`/api/jobs/enqueued`).then(jobs => {
        this.timeout = setTimeout(this.update, getIntervalTime());
        this.setState({ queues: jobs });
    });

    render(_, { queues }: EnqueuedJobsListState) {
        if (queues === undefined || queues === null) return (<Loading/>);
        return (queues.map(queue => (
            <div>
                <h4>{queue.name}</h4>
                <ul>
                    {queue.jobs.map(job => (<li><code>{job.method}</code></li>))}
                </ul>
            </div>
        )));
    }
}

interface ProcessingJobsListState {
    jobs: Job[]
}

class ProcessingJobsList extends Component<{}, ProcessingJobsListState> {
    timeout: any = null;
    componentDidMount() {
        this.update();
    }
    componentWillUnmount() {
        if (this.timeout) clearTimeout(this.timeout);
    }
    update = () => http.getJson<Job[]>(`/api/jobs/processing`).then(jobs => {
        this.timeout = setTimeout(this.update, getIntervalTime());
        this.setState({ jobs: jobs });
    });

    render(_, { jobs }: ProcessingJobsListState) {
        if (jobs === undefined || jobs === null) return (<Loading/>);
        return (
            <ul>
                {jobs.map(job => (<li><code>{job.method}</code></li>))}
            </ul>
        );
    }
}

interface FailedJobsListState {
    queues: JobQueue<FailedJob>[]
}

class FailedJobsList extends Component<{}, FailedJobsListState> {
    timeout: any = null;
    componentDidMount() {
        this.update();
    }
    componentWillUnmount() {
        if (this.timeout) clearTimeout(this.timeout);
    }
    update = () => http.getJson<JobQueue<FailedJob>[]>(`/api/jobs/failed`).then(jobs => {
        this.timeout = setTimeout(this.update, getIntervalTime());
        this.setState({ queues: jobs });
    });

    render(_, { queues }: FailedJobsListState) {
        if (queues === undefined || queues === null) return (<Loading/>);
        return (queues.map(queue => (
            <div>
                <h4>{queue.name}</h4>
                <ul>
                    {queue.jobs.map(job => (
                        <li>
                            <code>{job.method}</code>
                            <div>{job.exceptionType}: {job.exceptionMessage}</div>
                        </li>
                    ))}
                </ul>
            </div>
        )));
    }
}

interface SucceededJobsListState {
    jobs: SucceededJob[]
}

class SucceededJobsList extends Component<{}, SucceededJobsListState> {
    timeout: any = null;
    componentDidMount() {
        this.update();
    }
    componentWillUnmount() {
        if (this.timeout) clearTimeout(this.timeout);
    }
    update = () => http.getJson<SucceededJob[]>(`/api/jobs/succeeded`).then(jobs => {
        this.timeout = setTimeout(this.update, getIntervalTime());
        this.setState({ jobs });
    });
    render(_, { jobs }: SucceededJobsListState) {
        if (jobs === undefined || jobs === null) return (<Loading/>);
        return (<ul>
                {jobs.map(job => (
                    <li>
                        <code>{job.method}</code> completed in {job.executionTime}ms
                    </li>
                ))}
            </ul>
        );
    }
}

const Jobs = () => (
    <div class={style.page}>
        <h2>Jobs</h2>
        <JobView type="enqueued" children={<EnqueuedJobsList/>}/>
        <JobView type="processing" children={<ProcessingJobsList/>}/>
        <JobView type="failed" children={<FailedJobsList/>}/>
        <JobView type="succeeded" children={<SucceededJobsList/>}/>
    </div>
);
export default Jobs;