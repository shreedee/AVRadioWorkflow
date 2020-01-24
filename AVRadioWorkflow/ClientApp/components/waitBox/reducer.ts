import ReducerBase from '../../bootCommon/baseReducer';
import { handleAction, handleActions } from 'redux-actions';
import { IAsyncResult } from '../../bootCommon/asyncStateMiddleware';

import * as _ from 'lodash';

type WaitProgress = {
    status?: string;
    percent?: number;
};

export interface IWaitBox {
    readonly waitStatusAsync: IAsyncResult<boolean>;
    readonly description: string;
    readonly waitProgress?: WaitProgress; 

}

export interface IWaitBoxState {
    readonly currentWaits: { [key: string]: IWaitBox }
}

type myActions = {
    cancelWait: (waitId: string) => string;
    executeWait: (waitId: string, description: string, loadPromise: PromiseLike<boolean>) => boolean;

    setProgress: (waitId: string, progress?: WaitProgress) => WaitProgress;
}

class WaitReducer extends ReducerBase<IWaitBoxState, myActions>{

    createActionList() {
        return {
            cancelWait: (key: string) =>key,
            executeWait: [
                (waitId: string, description: string, loadPromise: PromiseLike<boolean>) => loadPromise,
                (waitId: string, description: string, loadPromise: PromiseLike<boolean>) => ({ Async: true, waitId, description })
            ],

            setProgress: (waitId: string, progress?: WaitProgress) => ({ waitId, progress })
        };
    }
    
    reducers() {

        const waitListHandlers = {};

        waitListHandlers[this._myActions.executeWait.toString()] = (state, action) => {
            let currentState = _.clone((state || {}) as { [key: string]: IWaitBox });
            const payload = action.payload as IAsyncResult<boolean>;
            
            
            const meta = action.meta as { waitId: string, description: string };

            if (payload.isLoading || payload.error) {
                
                    currentState[meta.waitId] = { waitStatusAsync: payload, description: meta.description };
                
            } else {
                if (currentState[meta.waitId])
                    delete currentState[meta.waitId];
            }

            return currentState;
        }

        waitListHandlers[this._myActions.cancelWait.toString()] = (state, action) => {
            let currentState = _.clone((state || {}) as { [key: string]: IWaitBox });
            const key = action.payload as string;

            if (!currentState[key] || currentState[key].waitStatusAsync.isLoading)
                return state;

            delete currentState[key];

            return currentState;
        };

        waitListHandlers[this._myActions.setProgress.toString()] = (state, action) => {
            const currentState = _.clone((state || {}) as { [key: string]: IWaitBox });
            const payload = action.payload as { waitId: string, progress?: WaitProgress };

            const waitBoxState = currentState[payload.waitId];
            if (!(waitBoxState && waitBoxState.waitStatusAsync && waitBoxState.waitStatusAsync.isLoading))
                return state;

            currentState[payload.waitId] = _.assign([], waitBoxState, { waitProgress: payload.progress });

            return currentState;
        };

        return {
            currentWaits: handleActions(waitListHandlers, []),
        };
        
    }

    private _lastWaitNumber: number = 0;

    doWait = (description: string, loader: PromiseLike<boolean>) =>
        this._myActions.executeWait((++this._lastWaitNumber).toString(), description, loader);


    doWaitAsync = (description: string, loaderCreater: () => PromiseLike<boolean>) =>
        this.doWait(description, loaderCreater());

    InitWait(description: string, loader: PromiseLike<boolean>) {
        return (dispatch, getState) => {
            const waitId = (++this._lastWaitNumber).toString();
            const waitPromise = dispatch(this._myActions.executeWait(waitId, description, loader));

            //return _.assign({}, waitPromise, { waitId });
            return {
                waitPromise,
                waitId
            };
            
        };
    }

    setProgress = (waitId: string, progress?: WaitProgress) => this._myActions.setProgress(waitId, progress);
        
    closeWait = (waitId: string) => this._myActions.cancelWait(waitId);

}


export default () => WaitReducer.getInstance(WaitReducer, 'waitBox'); 

