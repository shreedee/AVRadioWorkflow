import * as React from 'react';
import { InputGroup, FormControl, FormGroup } from 'react-bootstrap';

import ensureLogin, { ILoginState } from './reducer';

type ConnectedProps = ILoginState;

const LocationView: React.SFC<ConnectedProps & { dispatch }> = ({ creds, dispatch }) => {

    return <div>
        <FormGroup><InputGroup >
            <InputGroup.Prepend>
                <InputGroup.Text id="my_nick">User name</InputGroup.Text>
            </InputGroup.Prepend>
            <FormControl

                placeholder="Wordpress username"
                aria-label="your name"
                aria-describedby="my_nick"

                required

                value={creds && creds.username || ''}

                onChange={e => dispatch(ensureLogin().setCredsProp('username', e.target.value))}
            />
        </InputGroup></FormGroup>

        <FormGroup><InputGroup >
            <InputGroup.Prepend>
                <InputGroup.Text id="my_seat">Password</InputGroup.Text>
            </InputGroup.Prepend>
            <FormControl

                type="password"

                placeholder="Your wordpress password"
                aria-label="password"
                aria-describedby="my_seat"

                required

                value={creds && creds.pwd || ''}

                onChange={e => dispatch(ensureLogin().setCredsProp('pwd', e.target.value))}
            />
        </InputGroup></FormGroup>

    </div>;

}

import { connect } from 'react-redux';
import { withPopup } from '../collectPopup';

export default withPopup({
    id: 'loginPopup',
    header: 'Signin Please'
},
    connect<ConnectedProps, { dispatch }, {}>((state) => {
        return ensureLogin().getCurrentState(state);
    })(LocationView));


