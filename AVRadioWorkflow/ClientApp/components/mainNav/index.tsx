import * as React from 'react';
import { Navbar, Nav, Container } from 'react-bootstrap';
import { LinkContainer } from 'react-router-bootstrap';

import ensureLogin, { ILoginState } from '../login/reducer';

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSignOutAlt  } from '@fortawesome/free-solid-svg-icons';


type ConnectedProps = {
    readonly jwt: string;
};

const MainNav: React.SFC<ConnectedProps & { dispatch }> = ({ jwt, dispatch }) => {
    return <Navbar bg="light" expand="lg">
        <LinkContainer to="/"><Navbar.Brand >AVRadio Workflow</Navbar.Brand></LinkContainer>
        <Navbar.Toggle aria-controls="basic-navbar-nav" />
        <Navbar.Collapse id="basic-navbar-nav">
            <Nav className="mr-auto">
                <LinkContainer to="/publiMe"><Nav.Link>PubliMe</Nav.Link></LinkContainer>
                <LinkContainer to="/foldercreator"><Nav.Link >Create new folder</Nav.Link></LinkContainer>
            </Nav>

            <Nav className="mr-left">

                {jwt && <Nav.Link onClick={() => dispatch(ensureLogin().signOut())} ><FontAwesomeIcon icon={faSignOutAlt} />Sign out</Nav.Link>}

            </Nav>


        </Navbar.Collapse>
    </Navbar>;
}

import { connect } from 'react-redux';

export default connect<ConnectedProps, { dispatch }, {}>((state) => {
    const { jwt } = ensureLogin().getCurrentState(state);
    return { jwt };
})(MainNav);
