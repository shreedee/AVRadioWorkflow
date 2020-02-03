import * as React from 'react';
import {  Container } from 'react-bootstrap';
import './siteWide.scss';


import WaitBox from './components/waitBox';

import Login from './components/login';

import MainNav from './components/mainNav';


class LayoutView extends React.Component<{}, {}> {

    public render() {

        return <Container>
            <MainNav />
            <WaitBox />
            <Login/>
            <div className="mainContent">
                {this.props.children}
            </div>
        </Container>;
    }
}

export default LayoutView;
