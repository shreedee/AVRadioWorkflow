import ReducerBase from '../../bootCommon/baseReducer';
import { handleAction, handleActions } from 'redux-actions';

import * as _ from 'lodash';

type PromiseResolver = { resolve: () => void, reject: () => void, valueSaver?: () => PromiseLike<void>, err?:string, saving?:boolean} ;

export interface IPopupsState {
    readonly currentPopups: { [key: string]: PromiseResolver };
}

type myActions = {
    setPopup: (id: string, resolver?: PromiseResolver) => { id: string, resolver?: PromiseResolver };
    
}

class popupReducer extends ReducerBase<IPopupsState, myActions>{

    createActionList() {
        return {
            setPopup: (id: string, resolver?: PromiseResolver) => ({ id, resolver})
        };
    }

    reducers() {

        return {
            currentPopups: handleAction(this._myActions.setPopup, (state, action) => {
                const newState = _.clone(state || {}) as { [key: string]: PromiseResolver };
                const payload = action.payload as { id: string, resolver?: PromiseResolver };

                if (!payload.resolver)
                    delete newState[payload.id];
                else
                    newState[payload.id] = payload.resolver;

                return newState;

            }, {})
        };
    }

    closePopup(id: string, cancelClicked?: boolean) {
        const _mine = this;
        return (dispatch, getState) => {
            const { currentPopups } = _mine.getCurrentState(getState());

            if (!currentPopups[id])
                throw `popup id ${id}`;

            const { reject, resolve, valueSaver } = currentPopups[id];

            if (cancelClicked) {
                dispatch(_mine._myActions.setPopup(id, null));
                reject();
            } else {

                Promise.resolve()
                    .then(async () => {
                        if (valueSaver) {

                            dispatch(_mine._myActions.setPopup(id, _.assign({}, currentPopups[id], { saving: true })));

                            await valueSaver();
                        }

                        dispatch(_mine._myActions.setPopup(id, null));
                        resolve();
                    })
                    .catch(err => {
                        dispatch(_mine._myActions.setPopup(id, _.assign({}, currentPopups[id],
                            {
                                err: (err && (err.Message || err)) || 'unknown error',
                                saving: false
                            })));
                    })
                ;
            }
        };
    }

    ///throw from value saver if there is error
    doPopupAsync(id:string,valueSaver?: ()=>PromiseLike<void>) {
        const _mine = this;
        return (dispatch, getState) => {
            return new Promise<void>((resolve, reject) => {
                dispatch(_mine._myActions.setPopup(id, { resolve, reject, valueSaver} ));
            });
        }
    }

}

export default () => popupReducer.getInstance(popupReducer, 'collectPopup'); 