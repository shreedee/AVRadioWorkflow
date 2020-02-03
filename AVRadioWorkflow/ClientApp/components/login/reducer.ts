import ReducerBase from '../../bootCommon/baseReducer';
import { handleAction, handleActions } from 'redux-actions';

import ensurePopup from '../collectPopup/reducer';
import ensureWaitBox from '../waitBox/reducer';

import { checkFetchError } from '../../bootCommon/asyncLoader';

import { CredsModel } from '../../generated/CredsModel';

import * as _ from 'lodash';

export interface ILoginState {
    readonly jwt: string;
    readonly creds: CredsModel;
};

type myActions = {
    setJwt: (value?: string) => string;

    setCredsProp: (key: keyof CredsModel, value: any) => { key: keyof CredsModel, value: any };
}


let _jwtChecked = false;

class loginReducer extends ReducerBase<ILoginState, myActions>{

    createActionList() {
        return {
            setJwt: (value?: string) => value,
            setCredsProp: (key?: keyof CredsModel, value?: any) => !!key?({ key,value}):null,
        };
    }

    initialActions(dispatch, getState) {
        const saved = this.getPersistedData('jwt');
        if (!saved)
            return;

        dispatch(this._myActions.setJwt(saved));
    }

    /*
    initialActions(dispatch, getState) {

        const saved = this.getPersistedData('jwt');
        if (!saved)
            return;

        dispatch(this._myActions.setJwt(saved));

    
        setTimeout(()=>{
            const _mine = this;
            dispatch(ensureWaitBox().doWaitAsync('signing in', async () => {

                await checkFetchError(await fetch('/api/auth', {
                    method: 'get',
                    headers: {
                        Authorization: 'Bearer ' + saved
                    }
                }));

                //if we are here then JWT is good
                dispatch(_mine._myActions.setJwt(saved));

                return true;
            }));
        },100);

    
        
    }
    */
    

    reducers() {

        const saved = this.getPersistedData('jwt');

        return {

            jwt: handleAction(this._myActions.setJwt, (state, action: {payload?:string}) => {
                this.persistData('jwt', action.payload||null);
                return action.payload||null;
            }, saved),

            creds: handleAction(this._myActions.setCredsProp, (state?: CredsModel, action?: { payload: { key: keyof CredsModel, value: any } }) => {

                //clear out the creds
                if (!action.payload)
                    return {};

                const newState = _.clone(state || {}) as CredsModel;

                newState[action.payload.key] = action.payload.value;

                return newState;
            }, null),

        };
    }

    setCredsProp = (key: keyof CredsModel, value: any) => this._myActions.setCredsProp(key, value);

    signOut = () => this._myActions.setJwt();

    //shows popup if needed
    ensureSignedIn() {
        const _mine = this;
        return async (dispatch, getState) => {

            {
                const { jwt } = _mine.getCurrentState(getState());

                if (jwt){

                    if (!_jwtChecked) {

                        await dispatch(ensureWaitBox().doWaitAsync('signing in', async () => {

                            try {
                                await checkFetchError(await fetch('/api/auth', {
                                    method: 'get',
                                    headers: {
                                        Authorization: 'Bearer ' + jwt
                                    }
                                }));

                                _jwtChecked = true;
                            }
                            catch{
                                dispatch(_mine._myActions.setJwt());
                            }

                            return true;
                        }));
                    }

                    if (_jwtChecked )
                        return jwt;
                }

                await dispatch(ensurePopup().doPopupAsync('loginPopup', async () => {
                    const { creds } = _mine.getCurrentState(getState());

                    if (!creds || !creds.username || !creds.pwd)
                        throw 'Username and password are required';

                    var fected = await checkFetchError(await fetch(`/api/auth`, {
                        method: 'post',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(creds)
                    }));

                    const jwt = await fected.text() as string;

                    dispatch(_mine._myActions.setJwt(jwt));
                }));
            }


            const { jwt } = _mine.getCurrentState(getState());
            return jwt;
        }
    }

}

export default () => loginReducer.getInstance(loginReducer, 'login'); 
