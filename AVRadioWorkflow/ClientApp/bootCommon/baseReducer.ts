import { createActions } from 'redux-actions';
import { combineReducers } from 'redux';

import { createInjectStore, injectReducer } from './ReduxInjector';

import * as _ from 'lodash';

import { createAction, handleAction } from 'redux-actions';

import * as commonDefinations from './commonDefinations';

// This type can be used as a hint on action creators so that its 'dispatch' and 'getState' params are
// correctly typed to match your store.
export interface AppThunkAction<TAction> {
    (dispatch: (action: TAction) => void, getState: () => any): void;
}

let _instanceMap = {};


export function createInjectableStore(reducers, initialState, ...args) {

    
    if (initialState) {
        //we are rehydrating on the client. don't cleanup
        //leave existing reducres alone
        console.debug('injectreducer: hydrating');
    } else {
        //clear all the static reducres so that server initializes them again
        _instanceMap = {};
        console.debug('injectreducer: reseting reducers');
    }
    
    const existingReducerKeys = _.keys(reducers || {});
    const nonExisting = _.filter(_.keys(initialState || {}), key => !_.includes(existingReducerKeys, key))

    let dummyReducers = {};
    _.each(nonExisting, key => {
        dummyReducers[key] = handleAction(createAction('DUMMYACTION_' + key), (state, action) => {
            
            return null;
        }, null)
    });

    const allReducers = _.assign(dummyReducers, reducers);


    return createInjectStore(allReducers, initialState, ...args);
}

//Used to create reusable reducer functions
export abstract class reducerHelperBase<TState, TAction>{
    //Our location in the store
    protected readonly _storeKeysDelegate: () => string[];

    constructor(storeKeysDelegate: ()=>string[]) {
        this._storeKeysDelegate = storeKeysDelegate;
    }

    abstract createActionList(): {
        readonly [P in keyof TAction]: any;
    };

    protected _myActions: TAction = null;

    //used by derived classes to get state during thunked actions
    protected getCurrentState(getState:()=>any) {
        return commonDefinations.getCurrentState<TState>(getState, this._storeKeysDelegate());
    }

    //used to connect helper state to Redux Connect function
    public getmyState(globalstate: any) {
        return commonDefinations.parseState<TState>(globalstate, this._storeKeysDelegate());
    }


    protected abstract reducers(): {
        readonly [P in keyof TState]: any;
    };

    //invoked by containing reducer
    getReducers(storeActions: TAction) {
        this._myActions = storeActions;
        return combineReducers <TState>(this.reducers() as any);
    }

    protected getPersistedData(key: string) {
        return commonDefinations.getPersistedData(this._storeKeysDelegate().join('_') + '_' + key);
    }

    protected persistData(key: string, value: any) {
        return commonDefinations.persistData(this._storeKeysDelegate().join('_') + '_' + key, value);
    }
}


abstract class ReducerBase<TState, TAction = any> {

    
    protected abstract createActionList(): {
        readonly [P in keyof TAction]: any;
    };

    protected abstract reducers(): {
        readonly [P in keyof TState]: any;
    };

    protected initialActions(dispatch: any, getState: () => any) {
        //the default Method does Notning
    }
    
    protected _myActions: TAction;

    private _NameinStore: string;

    public getMyNameinStore() { return this._NameinStore;}

    private initialize(name:string) {

        let actions = {};
        this._NameinStore = name;
        
        actions[name] = this.createActionList();

        this._myActions = createActions(actions)[name];

        //need to fudge the reducers type cause we can't force reducermappbject
        // note we haev pull request for this, right now I fix this by hand
        injectReducer(name, combineReducers<TState>(this.reducers() as any), true, this.initialActions.bind(this));
        console.debug('injectreducer: injectReducer called');

        _instanceMap[name] = this;

    }
    
    public static getInstance<TState, T extends ReducerBase<TState>>(c: { new(): T; }, name:string): T {

        //console.log('injectreducer: getInstance called');

        if (_instanceMap[name])
            return _instanceMap[name];
        else {
            let t = new c();
            t.initialize(name);
            return t;
        }
        
    }

    public getCurrentState(state: {}): TState {
        return state[this._NameinStore] || {}
    }

    protected getPersistedData(key: string) {
        return commonDefinations.getPersistedData(this._NameinStore + '_' + key);
    }

    protected persistData(key: string, value: any) {
        return commonDefinations.persistData(this._NameinStore + '_' + key, value);
    }

}

export default ReducerBase;


