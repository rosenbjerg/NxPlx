export type MessageHandler = (msg: Message) => void;

export interface Message {
    type:string
    data:any
}

export interface Connection {
    send(message:Message):void

    subscribe(type:string, handler:(msg:Message) => void)
    unsubscribe(type:string, handler:(msg:Message) => void)
}

export default class WebsocketMessenger implements Connection {

    public static Get () {
        const w = window as any;
        if (w.__websocketmanager !== undefined) {
            return w.__websocketmanager;
        }
        return w.__websocketmanager = new WebsocketMessenger();
    }

    private webSocket:WebSocket;
    private connected = false;

    private unsent:Message[] = [];

    private handlers:{ [index:string]:MessageHandler[] } = {};

    constructor() {
        const protocol = location.protocol === 'https' ? 'wss' : 'ws';
        this.webSocket = new WebSocket(`${protocol}://${location.host}/api/broadcast`);
        this.webSocket.addEventListener('open', this.onOpen);
        this.webSocket.addEventListener('close', this.onClose);
        this.webSocket.addEventListener('message', this.onMessage);

    }
    public send(message:Message): void {
        if (this.connected) {
            this.webSocket.send(JSON.stringify(message));
        } else {
            this.unsent.push(message);
        }
    }

    public subscribe(type:string, handler:MessageHandler) {
        let handlers = this.handlers[type];
        if (!handlers) handlers = this.handlers[type] = [];
        handlers.push(handler);
    }

    public unsubscribe(type:string, handler:MessageHandler) {
        const handlers = this.handlers[type] || [];
        handlers.splice(handlers.indexOf(handler), 1);
    }

    private onOpen() {
        console.log('connected');
        this.connected = true;
        const unsent = [ ...this.unsent ];
        this.unsent = [];
        unsent.forEach(this.send);
    }

    private onClose() {
        console.log('disconnected');
        this.connected = false;
    }

    private onMessage(ev:MessageEvent) {
        const data = ev.data;
        console.log('received', data);
    }
}