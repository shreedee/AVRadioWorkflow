import * as React from 'react';

import { Card, Form, Row, Col, InputGroup, Button, Alert } from 'react-bootstrap';

import { withRouter, RouteComponentProps } from 'react-router-dom';

import DatePicker from 'react-datepicker';
import "react-datepicker/dist/react-datepicker.css";

import ensureCreator, { ICreatorState } from './reducer';

import queryString from 'query-string';

import MediaList from '../mediaList';

import "./styles.scss";

type ConnectedProps = ICreatorState;

type ComponentProps = RouteComponentProps<{}>;

class PubliMeView extends React.PureComponent<ConnectedProps & ComponentProps & { dispatch }, {}>{
    componentDidMount() {
        const { dispatch, location } = this.props;

        const { filename } = queryString.parse(location && location.search);

        dispatch(ensureCreator().loadStuff(filename as string));
    }

    render() {
        return <div className="folderCreator">
        </div>;
    }
}

import { connect } from 'react-redux';

export default withRouter(connect<ConnectedProps, { dispatch }, {}>((state) => {
    return ensureCreator().getCurrentState(state);
})(PubliMeView));

