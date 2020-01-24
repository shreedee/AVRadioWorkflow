//contains common utility methods
import * as _ from "lodash";

//gets the current object from global state using key array
export function getCurrentState<T>(getState:()=>any, storeKeys:string[]): T {
    let state = getState() || {};


    return parseState<T>(state, storeKeys);
}

export function parseState<T>(globalState: any, storeKeys: string[]): T {
    let state = globalState;

    _.each(storeKeys, k => {
        state = state[k] || {}
    });

    return state;
}

export function getPersistedData(item_key: string) {
    if (typeof localStorage === "undefined") {
        console.warn('no local storage found');
        return null;
    }
    
    const strignData = localStorage.getItem(item_key);

    if (!strignData)
        return null;

    return JSON.parse(strignData);
}

export function persistData(item_key: string, value: any) {
    if (typeof localStorage === "undefined") {
        console.warn('no local storage found');
        return;
    }

    if (null == value || typeof value === "undefined")
        localStorage.removeItem(item_key);
    else {
        localStorage.setItem(item_key, JSON.stringify(value));
    }

    //return the value so that call can be changed
    return value;
}
